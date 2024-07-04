namespace Wisp.Tests;

public sealed partial class CosDocumentTests
{
    public sealed class Objects
    {
        [Fact]
        public void Should_Resolve_Indirect_Object_From_Object_Stream()
        {
            // Given
            var fixture = CosDocumentFixture.Simple.Create();
            var document = fixture.Document;

            // When
            var success = document.Objects.TryGet(7, 0, out var obj);

            // Then
            Assert.True(success);
            Assert.NotNull(obj);

            obj.Object.ShouldBeOfType<CosDictionary>().And(dict =>
            {
                dict.Get<CosInteger>(CosNames.Count).ShouldHaveValue(2);
                dict.Get<CosObjectReference>(CosNames.First).ShouldBe(8, 0);
                dict.Get<CosObjectReference>(CosNames.Last).ShouldBe(8, 0);
            });
        }
    }
}