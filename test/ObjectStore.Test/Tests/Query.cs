﻿namespace ObjectStore.Test.Tests
{
    public enum Query
    {
        Insert,
        Update,
        Delete,
        DeleteSub,
        Select,
        OrderBy,
        SimpleExpressionEqual,
        SimpleExpressionEqualToNull,
        SimpleExpressionUnequal,
        SimpleExpressionUnequalToNull,
        SimpleExpressionAdd,
        SimpleExpressionSubtract,
        SimpleExpressionGreater,
        SimpleExpressionGreaterEqual,
        SimpleExpressionLess,
        SimpleExpressionLessEqual,
        SimpleExpressionConstantValue,
        SimpleExpressionContains,
        SimpleExpressionAnd,
        ForeignObjectEqual,
        ForeignObjectPropertyEqualTo
    }
}
