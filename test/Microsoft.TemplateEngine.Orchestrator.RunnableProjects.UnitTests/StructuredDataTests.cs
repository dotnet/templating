using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.TestHelper;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests
{
    public class StructuredDataTests : TestBase
    {
        [Fact(DisplayName = nameof(StructuredData_TestDeserialize))]
        public void StructuredData_TestDeserialize()
        {
            const string obj = @"{
    ""integer"": 1,
    ""string"": ""test"",
    ""bool"": true,
    ""array"": [
        {""a"": ""b""},
        [1],
        true,
        ""test"",
        2
    ]
}";

            JToken data = JToken.Parse(obj);
            //Run the converter
            StructuredData d = data.ToObject<StructuredData>();

            //Verify that we got an object
            Assert.True(d.IsObjectData);
            Assert.False(d.IsArrayData);
            Assert.False(d.IsPrimitive);

            //Verify that we have the expected number of keys
            Assert.Equal(4, d.Keys.Count);

            //Verify that getting a value by index on an object behaves nicely
            Assert.False(d.TryGetValueByIndex(0, out IStructuredData _));
            //Verify that getting a value by name on an object gets the value
            Assert.True(d.TryGetNamedValue("integer", out IStructuredData currentValue));
            //Verify that value for objects is null
            Assert.Null(d.Value);

            //Verify that the value we got is non-null
            Assert.NotNull(currentValue);

            //The value we got should be an integer - which should be classified as a primitive
            Assert.False(currentValue.IsObjectData);
            Assert.False(currentValue.IsArrayData);
            Assert.True(currentValue.IsPrimitive);

            //We should have gotten the number 1
            Assert.Equal(1L, currentValue.Value);

            //Verify that asking for a missing key behaves nicely
            Assert.False(d.TryGetNamedValue("missing", out IStructuredData _));

            //Try to get the array property
            Assert.True(d.TryGetNamedValue("array", out IStructuredData arrayData));

            //Verify that it's an array
            Assert.False(arrayData.IsObjectData);
            Assert.True(arrayData.IsArrayData);
            Assert.False(arrayData.IsPrimitive);

            Assert.True(arrayData.TryGetValueByIndex(0, out IStructuredData arrayElement0));
            Assert.True(arrayElement0.IsObjectData);

            Assert.True(arrayData.TryGetValueByIndex(1, out IStructuredData arrayElement1));
            Assert.True(arrayElement1.IsArrayData);
            Assert.True(arrayElement1.TryGetValueByIndex(0, out IStructuredData arrayElement1Element0));
            Assert.True(arrayElement1Element0.IsPrimitive);
            Assert.Equal(1L, arrayElement1Element0.Value);
        }
    }
}
