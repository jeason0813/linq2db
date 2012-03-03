﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Data.Linq;
	using Extensions;

	public class Table<T> : IOrderedQueryable<T>, IQueryProvider
	{
		#region Init

		public Table(IDataContextInfo dataContextInfo, Expression expression)
		{
#if SILVERLIGHT
			if (dataContextInfo == null) throw new ArgumentNullException("dataContextInfo");

			DataContextInfo = dataContextInfo;
#else
			DataContextInfo = dataContextInfo ?? new DefaultDataContextInfo();
#endif
			Expression      = expression      ?? Expression.Constant(this);
		}

		public Table(IDataContextInfo dataContextInfo)
			: this(dataContextInfo, null)
		{
		}

#if !SILVERLIGHT

		public Table()
			: this((IDataContextInfo)null, null)
		{
		}

		public Table(Expression expression)
			: this((IDataContextInfo)null, expression)
		{
		}

#endif

		public Table(IDataContext dataContext)
			: this(dataContext == null ? null : new DataContextInfo(dataContext), null)
		{
		}

		public Table(IDataContext dataContext, Expression expression)
			: this(dataContext == null ? null : new DataContextInfo(dataContext), expression)
		{
		}

		[NotNull] public Expression       Expression      { get; set; }
		[NotNull] public IDataContextInfo DataContextInfo { get; set; }

		internal  Query<T> Info;
		internal  object[] Parameters;

		#endregion

		#region Public Members

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string _sqlTextHolder;

// ReSharper disable InconsistentNaming
		[UsedImplicitly]
		private string _sqlText { get { return SqlText; }}
// ReSharper restore InconsistentNaming

		public  string  SqlText
		{
			get
			{
				if (_sqlTextHolder == null)
				{
					var info = GetQuery(Expression, true);
					_sqlTextHolder = info.GetSqlText(DataContextInfo.DataContext, Expression, Parameters, 0);
				}

				return _sqlTextHolder;
			}
		}

		#endregion

		#region Execute

		IEnumerable<T> Execute(IDataContextInfo dataContextInfo, Expression expression)
		{
			return GetQuery(expression, true).GetIEnumerable(null, dataContextInfo, expression, Parameters);
		}

		Query<T> GetQuery(Expression expression, bool cache)
		{
			if (cache && Info != null)
				return Info;

			var info = Query<T>.GetQuery(DataContextInfo, expression);

			if (cache)
				Info = info;

			return info;
		}

		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return "Table(" + typeof (T).Name + ")";
		}

#endif

		#endregion

		#region IQueryable Members

		Type IQueryable.ElementType
		{
			get { return typeof(T); }
		}

		Expression IQueryable.Expression
		{
			get { return Expression; }
		}

		IQueryProvider IQueryable.Provider
		{
			get { return this; }
		}

		#endregion

		#region IQueryProvider Members

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			return new ExpressionQuery<TElement>(DataContextInfo, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator.CreateInstance(typeof(ExpressionQuery<>).MakeGenericType(elementType), new object[] { this.DataContextInfo, expression });
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return (TResult)GetQuery(expression, false).GetElement(null, DataContextInfo, expression, Parameters);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return GetQuery(expression, false).GetElement(null, DataContextInfo, expression, Parameters);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return Execute(DataContextInfo, Expression).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Execute(DataContextInfo, Expression).GetEnumerator();
		}

		#endregion
	}
}
