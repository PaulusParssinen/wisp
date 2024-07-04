namespace Wisp.Tests;

public sealed partial class CosDocumentTests
{
    public sealed class Info(CosDocumentFixture.XRefStream fixture) : IClassFixture<CosDocumentFixture.XRefStream>
    {
        [Fact]
        public void Should_Read_Info_Properties_Correctly()
        {
            // Given, When
            var obj = fixture.Document.Info;

            // Then
            obj.ShouldNotBeNull();
            obj.Id.ShouldBe(23, 0);
            obj.Title.ShouldHaveValue("PDF_Test.txt");
            obj.Creator.ShouldHaveValue("Adobe Acrobat 23.0");
            obj.Producer.ShouldHaveValue("Acrobat Web Capture 15.0");
            obj.CreationDate.ShouldHaveDate("20240227220231+01:00");
            obj.ModDate.ShouldHaveDate("20240227220240+01:00");
        }
    }
}