using System;
using static ObjectStore.DataBaseInitializer;

namespace ObjectStore.Database
{
    public interface IDatabaseInitializer
    {
        void RegisterConstraintStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc);

        void RegisterFieldStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc);

        void RegisterTableStatement(Func<IStatement, bool> predicate, Func<IStatement, string, string> parseFunc);
    }
}
