using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.OData;
using Newtonsoft.Json.Linq;

namespace ODataGraphDelta {    

    public class TypedGraphDelta {

        private readonly TypedDelta _root;
        private readonly IDictionary<String, TypedGraphDelta> _children;

        public TypedGraphDelta(Type type, JObject json) {

            if(type == null) { throw new ArgumentNullException("type"); }
            if(json == null) { throw new ArgumentNullException("json"); }

            _root = TypedGraphDelta.ReadRoot(type, json);
            _children = TypedGraphDelta.ReadChildren(type, json);
        }

        public TypedDelta Root { get { return _root; } }
        public IDictionary<String, TypedGraphDelta> Children { get { return _children; } }

        public void Patch(object model) {

            if(model == null) { throw new ArgumentNullException("model"); }

            var type = model.GetType();

            //NOTE When using Entity Framework the entities returned are usually proxies extending your code first
            //entities, which would break an exact comparison here.
            if(!_root.EntityType.IsAssignableFrom(type)) { 
                throw new InvalidOperationException(
                    String.Format("Model is of type {0} but delta is for {1}", type.FullName, _root.EntityType.FullName)
                );
            }

            TypedGraphDelta.Patch(_root, model);

            foreach(var child in _children) {

                var property = type.GetProperty(child.Key);
                object value = null;

                //If there is a delta, then apply it to the existing value (or newly created value), otherwise we
                //set the property to null.
                if(child.Value != null) {

                    value = property.GetValue(model);

                    if(value == null) {
                        value = Activator.CreateInstance(property.PropertyType);
                    }

                    child.Value.Patch(value);
                }

                property.SetValue(model, value);
            }
        }

        #region Helpers

        private static TypedDelta ReadRoot(Type type, JObject json) {

            var root = TypedGraphDelta.CreateDelta(type);

            var properties = from property in type.GetProperties()
                             where 
                                property.PropertyType.IsValueType 
                                || property.PropertyType == typeof(String)
                                || property.PropertyType == typeof(byte[])
                             select property;

            foreach(var property in properties) {

                var path = String.Format("$['{0}']", property.Name);
                var value = json.SelectToken(path) as JValue;

                if(value != null) {
                    
                    var conversionType = TypedGraphDelta.GetConversionType(property.PropertyType);
                    object convertedValue = null;

                    if(value.Value != null) {
                        if(conversionType == typeof(byte[])) {
                            convertedValue = Convert.FromBase64String((string)value.Value);
                        } else {
                            convertedValue = Convert.ChangeType(value.Value, conversionType);
                        }
                    }

                    root.TrySetPropertyValue(
                        property.Name,
                        convertedValue
                    );
                }
            }

            return root;
        }

        private static IDictionary<String, TypedGraphDelta> ReadChildren(Type type, JObject json) {

            var children = new Dictionary<String, TypedGraphDelta>();

            var properties = from property in type.GetProperties()
                             where 
                                !property.PropertyType.IsValueType 
                                && property.PropertyType != typeof(String)
                                && property.PropertyType != typeof(byte[])
                             select property;

            foreach(var property in properties) {

                var path = String.Format("$['{0}']", property.Name);
                var value = json.SelectToken(path);

                if(value is JObject) {
                    children.Add(property.Name, new TypedGraphDelta(property.PropertyType, (JObject)value));
                } else if(value is JValue) {
                    if(((JValue)value).Value == null) { //If the object were not null it would be returned as JObject
                        children.Add(property.Name, null);
                    }
                }
            }

            return children;
        }

        private static void Patch(TypedDelta delta, object model) {

            var type = typeof(Delta<>).MakeGenericType(delta.EntityType);
            var method = type.GetMethod("Patch");

            method.Invoke(delta, new object[] { model });
        }

        private static TypedDelta CreateDelta(Type type) {
            return (TypedDelta)Activator.CreateInstance(typeof(Delta<>).MakeGenericType(type));
        }

        private static Type GetConversionType(Type propertyType) {

            //Convert.ChangeType cannot do conversions from T to Nullable<T>, but an implicit conversion will exist anyway
            //so convert to the underlying type instead.

            return (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                ? propertyType.GetGenericArguments().First()
                : propertyType;
        }

        #endregion
    }    
}
