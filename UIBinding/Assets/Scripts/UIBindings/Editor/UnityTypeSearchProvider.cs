using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Editor
{
    class TypeSearchProvider2 : SearchProvider
    {
        const string k_AssemblyToken = "asm";
        const string k_NameToken = "name";
        const string k_NamespaceToken = "ns";
        const string k_NotifyToken = "notify";

        readonly Type m_BaseType;
        readonly HashSet<Assembly> m_Assemblies = new();
        readonly QueryEngine<Type> m_QueryEngine = new();

        public TypeSearchProvider2(Type baseType) : base("type", "Type")
        {
            m_BaseType = baseType;

            // Propositions are used to provide the search filter options in the menu.
            fetchPropositions = FetchPropositions;

            // The actual items we search against.
            fetchItems = FetchItems;

            // The default table columns and the ones we show when reset is called.
            //tableConfig = GetDefaultTableConfig;

            // The additional available columns for this search provider.
            fetchColumns = FetchColumns;

            // The searchable data is what we search against when just typing in the search field.
            m_QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.AddFilter(k_AssemblyToken, o => o.Assembly.GetName().Name);
            m_QueryEngine.AddFilter(k_NameToken, o => o.Name);
            m_QueryEngine.AddFilter(k_NamespaceToken, o => o.Namespace);
            m_QueryEngine.AddFilter(k_NotifyToken, o => typeof(INotifyPropertyChanged).IsAssignableFrom( o ));
        }

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            yield return new SearchProposition(null, "Name", $"{k_NameToken}:", "Filter by type name.");
            yield return new SearchProposition(null, "Namespace", $"{k_NamespaceToken}:", "Filter by type namespace.");
            yield return new SearchProposition(null, "Support notify changes", $"{k_NotifyToken}=true", "Filter by notify changes support.");

            // We want to provide a list of all the assemblies that contain types derived from the base type.
            foreach (var asm in m_Assemblies)
            {
                var assemblyName = asm.GetName().Name;
                yield return new SearchProposition("Assembly", assemblyName, $"{k_AssemblyToken}={assemblyName}", "Filter by assembly name.");
            }
        }

        IEnumerator<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            if (context.empty)
                yield break;

            var query = m_QueryEngine.ParseQuery(context.searchQuery);
            if (!query.valid)
                yield break;

            var filteredObjects = query.Apply(GetSearchData());
            foreach (var t in filteredObjects)
            {
                yield return provider.CreateItem(context, t.AssemblyQualifiedName, t.Name, t.FullName, null, t);
            }
        }

        IEnumerable<Type> GetSearchData()
        {
            // Ignore UI Builder types
            //var builderAssembly = GetType().Assembly;

            foreach (var t in GetTypesDerivedFrom(m_BaseType))
            {
                if (t.IsGenericType || t.IsArray || !t.IsVisible || t.IsSpecialName /*|| t.Assembly == builderAssembly*/)
                    continue;

                m_Assemblies.Add(t.Assembly);
                yield return t;
            }
        }

        static IEnumerable<Type> GetTypesDerivedFrom(Type type)
        {
            if (type != typeof(object))
            {
                foreach (var t in TypeCache.GetTypesDerivedFrom(type))
                {
                    yield return t;
                }
            }
            else
            {
                // We need special handling for the System.Object type as TypeCache.GetTypesDerivedFrom(object) misses some types, such as primitives.
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Get all types in the assembly
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    // In case of the assembly contains one or more types that cannot be loaded, simply ignore it
                    catch (ReflectionTypeLoadException)
                    {
                        continue;
                    }
                    foreach (var t in types)
                    {
                        yield return t;
                    }
                }
            }
        }

        static IEnumerable<string> GetSearchableData(Type t)
        {
            // The string that will be evaluated by default
            yield return t.Name;
            if( t.Namespace != null )
                yield return t.Namespace;
            yield return t.Assembly.GetName().Name;
        }

        static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            var defaultColumns = new List<SearchColumn>
            {
                new SearchColumn("Name", "label")
                {
                    width = 400
                }
            };
            defaultColumns.AddRange(FetchColumns(context, null));
            return new SearchTable("type", defaultColumns);
        }

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> searchDatas)
        {
            // Note: The getter is serialized into the window so we need to use a method
            // instead of a lambda or it will break when the window is reloaded.
            // For the same reasons you should avoid renaming the methods or moving them around.

            yield return new SearchColumn("Namespace")
            {
                getter = GetNamespace,
                width = 250
            };
            yield return new SearchColumn("Assembly")
            {            
                getter = GetAssemblyName,
                width = 250
            };
            yield return new SearchColumn("Notify")
                         {            
                                 getter = GetNotifyChangeFlag,
                                 width  = 250
                         };
        }

        static object GetNamespace(SearchColumnEventArgs args)
        {
            if (!(args.item.data is Type t))
                return null;
            return t.Namespace;
        }

        static object GetAssemblyName(SearchColumnEventArgs args)
        {
            if (!(args.item.data is Type t))
                return null;
            return t.Assembly.GetName().Name;
        }

        static object GetNotifyChangeFlag(SearchColumnEventArgs args)
        {
            if (!(args.item.data is Type t))
                return null;
            return typeof(INotifyPropertyChanged).IsAssignableFrom(t) ? true : false;
        }
    }
}