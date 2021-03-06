﻿using System;
using System.Collections.Generic;
using System.Linq;
using ObjectStore.Database;
using ObjectStore.Interfaces;
using ObjectStore.MappingOptions;

namespace ObjectStore.OrMapping
{
    public class RelationalObjectStore : IObjectProvider, IObjectRegistration
    {
        IDataBaseProvider _databaseProvider;
        Dictionary<Type, IObjectProvider> _relationalObjectProvider;
        MappingOptionsSet _mappingOptionsSet;
        readonly string _connectionString;
        readonly bool _autoregisterTypes;

        public RelationalObjectStore(string connectionString, IDataBaseProvider databaseProvider, MappingOptionsSet mappingOptionsSet, bool autoregister)
        {
            _relationalObjectProvider = new Dictionary<Type, IObjectProvider>();
            _connectionString = connectionString;
            _autoregisterTypes = autoregister;
            _databaseProvider = databaseProvider;
            _mappingOptionsSet = mappingOptionsSet;
        }

        public RelationalObjectStore(string connectionString, IDataBaseProvider databaseProvider)
            : this(connectionString, databaseProvider, new MappingOptionsSet().AddDefaultRules(), false)
        {
        }

        public RelationalObjectStore Register<T>() where T : class
        {
            if (!_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                IObjectProvider newProvider = new InheritensObjectProvider<T>(_connectionString, _databaseProvider, _mappingOptionsSet);
                _relationalObjectProvider[typeof(T)] = newProvider;

                System.Reflection.MethodInfo registerMethod = null;
                List<Type> subTypes = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                            .Where(x => x.GetCustomAttributes(typeof(ForeignObjectMappingAttribute), true).Any())
                                            .Select(x => x.PropertyType).Where(x => !_relationalObjectProvider.ContainsKey(x))
                                            .Distinct().ToList();
                subTypes.AddRange(
                    typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                            .Where(x => x.GetCustomAttributes(typeof(ReferenceListMappingAttribute), true).Any())
                                            .Select(x => x.PropertyType.GetGenericArguments().First()).Where(x => !_relationalObjectProvider.ContainsKey(x))
                                            .Distinct());

                foreach (Type subType in subTypes.Where(x => x != typeof(T)).Distinct())
                {
                    if(registerMethod == null)
                        registerMethod = typeof(RelationalObjectStore).GetMethod("Register");

                    System.Linq.Expressions.Expression.Lambda<Action>(
                        System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Constant(this), registerMethod.MakeGenericMethod(subType)))
                            .Compile()();
                }
            }
            return this;
        }

        public void InitializeDatabase(Action<IDatabaseInitializer> initFunc = null)
        {
            DataBaseInitializer initializer = _databaseProvider.GetDatabaseInitializer(_connectionString);
            initFunc?.Invoke(initializer);

            IEnumerable<Type> GetTypesOrderedCorrectly(IEnumerable<Type> types)
            {
                List<Type> typesList = types.ToList();
                while (typesList.Count > 0)
                {
                    for(int i = 0; i < typesList.Count; i++)
                    {
                        TypeMappingOptions typeMappingOptions = _mappingOptionsSet.GetTypeMappingOptions(typesList[i]);
                        if (!typeMappingOptions.MemberMappingOptions.OfType<ForeignObjectMappingOptions>()
                            .Where(x => typesList.Contains(x.ForeignObjectType))
                            .Any())
                        {
                            yield return typesList[i];
                            typesList.RemoveAt(i);
                        }
                    }

                }
            }

            foreach (Type type in GetTypesOrderedCorrectly(_relationalObjectProvider.Keys))
            {
                TypeMappingOptions typeMappingOptions = _mappingOptionsSet.GetTypeMappingOptions(type);
                initializer.AddTable(typeMappingOptions.TableName);
                foreach (FieldMappingOptions memberMappineOptions in typeMappingOptions.MemberMappingOptions.OfType<FieldMappingOptions>().OrderBy(x => x.IsPrimaryKey))
                {
                    if (memberMappineOptions.Type == MappingType.ReferenceListMapping)
                        continue;

                    if (memberMappineOptions is ForeignObjectMappingOptions foreignObjectMappingOptions)
                    {
                        initializer.AddField(foreignObjectMappingOptions.DatabaseFieldName, foreignObjectMappingOptions.KeyType);

                        FieldMappingOptions foreignFieldMappingOptions = foreignObjectMappingOptions.ForeignMember as FieldMappingOptions;
                        TypeMappingOptions foreignTypeMappingOptions = _mappingOptionsSet.GetTypeMappingOptions(foreignObjectMappingOptions.ForeignObjectType);

                        IEnumerable<ReferenceListMappingOptions> referenceListMappingOptions =
                            foreignTypeMappingOptions.MemberMappingOptions.OfType<ReferenceListMappingOptions>()
                                .Where(x => x.ForeignProperty == foreignObjectMappingOptions.Member);



                        initializer.AddForeignKey(
                            foreignTypeMappingOptions.TableName,
                            foreignFieldMappingOptions.DatabaseFieldName,
                            referenceListMappingOptions.Any(x => x.DeleteCascade));
                    }
                    else
                        initializer.AddField(memberMappineOptions.DatabaseFieldName, memberMappineOptions.Member.PropertyType);

                    if (memberMappineOptions.IsPrimaryKey)
                        initializer.SetIsKeyField(memberMappineOptions.IsReadonly);
                }
            }

            initializer.Flush();
        }

        #region IObjectProvider Members
        public IQueryable<T> GetQueryable<T>() where T : class
        {
            if (_autoregisterTypes && !_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                Register<T>();
#if DEBUG && !NETCOREAPP1_0
                System.Diagnostics.Trace.TraceError("Type '{0}' is not registered in Objectstore.", typeof(T).FullName);
#endif
            }

            return _relationalObjectProvider[typeof(T)].GetQueryable<T>();
        }

        public T CreateObject<T>() where T : class
        {
            if (_autoregisterTypes && !_relationalObjectProvider.ContainsKey(typeof(T)))
            {
                Register<T>();
#if DEBUG && !NETCOREAPP1_0
                System.Diagnostics.Trace.TraceError("Type '{0}' is not registered in Objectstore.", typeof(T).FullName);
#endif
            }

            return _relationalObjectProvider[typeof(T)].CreateObject<T>();
        }

        public bool SupportsType(Type type)
        {
            if (_autoregisterTypes)
                return true;

            return _relationalObjectProvider.ContainsKey(type) && _relationalObjectProvider[type].SupportsType(type);
        }
        #endregion

        #region IObjectRegistration Members

        IObjectRegistration IObjectRegistration.Register<T>()
        {
            return this.Register<T>();
        }

        #endregion
    }

    public interface IObjectRegistration
    {
        IObjectRegistration Register<T>() where T : class;
        void InitializeDatabase(Action<IDatabaseInitializer> initFunc = null);
    }
}
