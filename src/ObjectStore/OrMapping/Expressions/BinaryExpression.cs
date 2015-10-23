using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectStore.OrMapping.Expressions
{
    public abstract class BinaryExpression : Expression
    {

        internal BinaryExpression(Expression parentExpression) : base(parentExpression)
        {}

        public abstract Expression Left { get; }

        public abstract Expression Right { get; }
    }

    public class AndExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal AndExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) : 
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get 
            {
                return string.Format("({0}) AND ({1})", _left.SqlExpression, _right.SqlExpression); 
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class OrExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal OrExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("({0}) OR ({1})", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class EqualExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal EqualExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                if (_left.GetType() == typeof(NullExpression))
                    return string.Format("{0} IS NULL", _right.SqlExpression);
                else if (_right.GetType() == typeof(NullExpression))
                    return string.Format("{0} IS NULL", _left.SqlExpression);
                else
                    return string.Format("{0} = {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class NotEqualExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal NotEqualExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                if (_left.GetType() == typeof(NullExpression))
                    return string.Format("{0} IS NOT NULL", _right.SqlExpression);
                else if (_right.GetType() == typeof(NullExpression))
                    return string.Format("{0} IS NOT NULL", _left.SqlExpression);
                else
                    return string.Format("{0} != {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class LessThanOrEqualExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal LessThanOrEqualExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} <= {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class GraterThanOrEqualExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal GraterThanOrEqualExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} >= {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class LessThanExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal LessThanExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} < {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class GraterThanExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal GraterThanExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} > {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }
    public class SubtractExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal SubtractExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} - {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }

    public class AddExpression : BinaryExpression
    {
        protected Expression _right;
        protected Expression _left;

        internal AddExpression(System.Linq.Expressions.BinaryExpression source, Expression parentExpression, ParsedExpression parseExpression) :
            base(parentExpression)
        {
            _right = Expression.ParseExpression(source.Right, this, parseExpression);
            _left = Expression.ParseExpression(source.Left, this, parseExpression);
        }

        public override string SqlExpression
        {
            get
            {
                return string.Format("{0} + {1}", _left.SqlExpression, _right.SqlExpression);
            }
        }

        public override Expression Left
        {
            get { return _left; }
        }

        public override Expression Right
        {
            get { return _right; }
        }
    }
}
