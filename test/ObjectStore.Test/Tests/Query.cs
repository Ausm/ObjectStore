﻿namespace ObjectStore.Test.Tests
{
    public enum Query
    {
        Insert,
        InsertNonInitializedKeyEntitiy,
        InsertDifferentTypesEntity,
        InsertDifferentWritabilityLevels,
        InsertForeignObjectKeyEntity,
        Update,
        UpdateDifferentTypesEntity,
        UpdateDifferentWritabilityLevels,
        UpdateForeignObjectKeyEntity,
        Delete,
        DeleteSub,
        DeleteForeignObjectKeyEntity,
        Select,
        SelectSub,
        SelectSubTake10,
        SelectNonInitializedKeyEntitiy,
        SelectDifferentTypesEntity,
        SelectDifferentWritabilityLevels,
        SelectForeignObjectKeyEntity,
        OrderBy,
        OrderByDescending,
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
