using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaCapta.SharePoint.Client.Data
{
    public class SharePointExpressionVisitor : ExpressionVisitor
    {
        private StringBuilder stringBuilder;

        public SharePointExpressionVisitor()
        {

        }

        internal string Translate(Expression expression)
        {
            this.stringBuilder = new StringBuilder();
            this.Visit(expression);
            return this.stringBuilder.ToString();
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                this.stringBuilder.Append("SELECT * FROM (");
                this.Visit(m.Arguments[0]);
                this.stringBuilder.Append(") AS T WHERE ");
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            }

            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    this.stringBuilder.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            this.stringBuilder.Append("(");
            this.Visit(binaryExpression.Left);
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.And:
                    this.stringBuilder.Append(" AND ");
                    break;
                case ExpressionType.Or:
                    this.stringBuilder.Append(" OR");
                    break;
                case ExpressionType.Equal:
                    this.stringBuilder.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    this.stringBuilder.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    this.stringBuilder.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    this.stringBuilder.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    this.stringBuilder.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    this.stringBuilder.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", binaryExpression.NodeType));
            }
            this.Visit(binaryExpression.Right);
            this.stringBuilder.Append(")");
            return binaryExpression;
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            IQueryable q = constantExpression.Value as IQueryable;
            if (q != null)
            {
                // assume constant nodes w/ IQueryables are table references
                this.stringBuilder.Append("SELECT * FROM ");
                this.stringBuilder.Append(q.ElementType.Name);
            }
            else if (constantExpression.Value == null)
            {
                this.stringBuilder.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(constantExpression.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        this.stringBuilder.Append(((bool)constantExpression.Value) ? 1 : 0);
                        break;
                    case TypeCode.String:
                        this.stringBuilder.Append("'");
                        this.stringBuilder.Append(constantExpression.Value);
                        this.stringBuilder.Append("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", constantExpression.Value));
                    default:
                        this.stringBuilder.Append(constantExpression.Value);
                        break;
                }
            }
            return constantExpression;
        }

        protected override Expression VisitMemberAccess(MemberExpression memberExpression)
        {
            if (memberExpression.Expression != null && memberExpression.Expression.NodeType == ExpressionType.Parameter)
            {
                this.stringBuilder.Append(memberExpression.Member.Name);
                return memberExpression;
            }

            throw new NotSupportedException(string.Format("The member '{0}' is not supported", memberExpression.Member.Name));
        }
    }
}
