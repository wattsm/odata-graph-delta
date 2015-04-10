using System;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ODataGraphDelta.Tests {
    public class TypedGraphDelta_Facts {

        #region Models

        public class _SimpleModel {
            public int Id { get; set; }
            public string Value { get; set; }
            public int Count { get; set; }
        }

        public class _ComplexModel {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public _SimpleModel Child { get; set; }
        }

        public class _NullableModel {
            public int? Id { get; set; }
            public DateTime? Created { get; set; }
            public bool? TriState { get; set; }
        }

        public class _ByteModel {
            public byte[] Value { get; set; }
        }

        #endregion

        public class About_Constructor {

            [Fact]
            public void A_Null_Type_Throws_ArgumentNullException() {
                Assert.Throws<ArgumentNullException>(() => {
                    var delta = new TypedGraphDelta(null, JObject.Parse("{ }"));
                });
            }

            [Fact]
            public void A_Null_JObject_Throws_ArgumentNullException() {
                Assert.Throws<ArgumentNullException>(() => {
                    var delta = new TypedGraphDelta(typeof(_SimpleModel), null);
                });
            }
        }

        public class About_Patch_Method {

            [Fact]
            public void A_Null_Model_Throws_Exception() {
                Assert.Throws<ArgumentNullException>(() => {

                    var delta = new TypedGraphDelta(
                        typeof(_SimpleModel), 
                        JObject.Parse("{ 'Value': 'Updated' }")
                    );

                    delta.Patch(null);
                });
            }

            [Fact]
            public void Root_Level_Changes_Are_Applied_Correctly() {

                var model = new _SimpleModel() { Id = 1, Value = "Original", Count = 3 };
                var json = JObject.Parse("{ 'Value': 'Updated', 'Count': 5 }");
                var delta = new TypedGraphDelta(typeof(_SimpleModel), json);

                delta.Patch(model);

                Assert.Equal(1, model.Id);
                Assert.Equal("Updated", model.Value);
                Assert.Equal(5, model.Count);
            }

            [Fact]
            public void Child_Level_Changes_Are_Applied_Correctly() {

                var model = new _ComplexModel() { 
                    Id = 1, 
                    FirstName = "John", 
                    LastName = "Smith", 
                    Child = new _SimpleModel() { Id = 2, Value = "Original", Count = 3 } 
                };

                var json = JObject.Parse("{ 'LastName': 'Doe', 'Child': { 'Value': 'Updated', 'Count': 5 } }");
                var delta = new TypedGraphDelta(typeof(_ComplexModel), json);

                delta.Patch(model);

                Assert.Equal(1, model.Id);
                Assert.Equal("John", model.FirstName);
                Assert.Equal("Doe", model.LastName);
                Assert.NotNull(model.Child);
                Assert.Equal(2, model.Child.Id);
                Assert.Equal("Updated", model.Child.Value);
                Assert.Equal(5, model.Child.Count);
            }

            [Fact]
            public void Child_Models_Are_Created_If_Null() {

                var model = new _ComplexModel() { Id = 1, FirstName = "John", LastName = "Smith" };
                var json = JObject.Parse("{ 'LastName': 'Doe', 'Child': { 'Value': 'Updated', 'Count': 5 } }");
                var delta = new TypedGraphDelta(typeof(_ComplexModel), json);

                delta.Patch(model);

                Assert.Equal(1, model.Id);
                Assert.Equal("John", model.FirstName);
                Assert.Equal("Doe", model.LastName);
                Assert.NotNull(model.Child);
                Assert.Equal(0, model.Child.Id);
                Assert.Equal("Updated", model.Child.Value);
                Assert.Equal(5, model.Child.Count);
            }

            [Fact]
            public void Nullable_Values_Are_Set_Correctly() {

                var model = new _NullableModel();
                var json = JObject.Parse("{ 'Id': 10, 'Created': '2015-01-01T13:00:00', 'TriState': true }");
                var delta = new TypedGraphDelta(typeof(_NullableModel), json);

                delta.Patch(model);

                Assert.Equal(10, model.Id);
                Assert.Equal(DateTime.Parse("2015-01-01 13:00:00"), model.Created);
                Assert.Equal(true, model.TriState);
            }

            [Fact]
            public void Nullable_Values_Are_Unset_Correctly() {

                var model = new _NullableModel() { Id = 10, Created = DateTime.Now, TriState = true };
                var json = JObject.Parse("{ 'Id': null, 'Created': null, 'TriState': null }");
                var delta = new TypedGraphDelta(typeof(_NullableModel), json);

                delta.Patch(model);

                Assert.False(model.Id.HasValue);
                Assert.False(model.Created.HasValue);
                Assert.False(model.TriState.HasValue);
            }

            [Fact]
            public void Child_Models_Are_Unset_Correctly() {

                var model = new _ComplexModel() { Child = new _SimpleModel() };
                var json = JObject.Parse("{ 'Child': null }");
                var delta = new TypedGraphDelta(typeof(_ComplexModel), json);

                delta.Patch(model);

                Assert.Null(model.Child);
            }

            [Fact]
            public void Byte_Arrays_Are_Set_Correctly() {

                var bytes = Encoding.ASCII.GetBytes("The quick brown fox jumped over the lazy sheep dog.");
                var base64 = Convert.ToBase64String(bytes);

                var model = new _ByteModel();
                var json = JObject.Parse("{ 'Value': '" + base64 + "' }");
                var delta = new TypedGraphDelta(typeof(_ByteModel), json);

                delta.Patch(model);

                Assert.Equal(bytes, model.Value);
            }
        }


    }
}
