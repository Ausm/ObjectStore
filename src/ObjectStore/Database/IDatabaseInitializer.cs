using System;
using static ObjectStore.DataBaseInitializer;

namespace ObjectStore.Database
{
    public interface IDatabaseInitializer
    {
        void RegisterAddConstraintStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc);

        void RegisterAddFieldStatment(Func<IField, bool> predicate, Func<IField, string, string> parseFunc);

        void RegisterCreateTableStatement(Func<IStatement, bool> predicate, Func<IStatement, string, string> parseFunc);
    }
}
