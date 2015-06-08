using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaCapta.SharePoint.Client.Data
{
    public abstract class SharePointQueryProvider : IQueryProvider
    {
        protected SharePointQueryProvider()
        {
        }

        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new SharePointQuery<S>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {            
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(SharePointQuery<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        S IQueryProvider.Execute<S>(Expression expression)
        {
            return (S)this.Execute(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return this.Execute(expression);
        }

        public abstract string GetQueryText(Expression expression);
        public abstract object Execute(Expression expression);
    }
}
