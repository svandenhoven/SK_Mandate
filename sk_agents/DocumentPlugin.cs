using Microsoft.SemanticKernel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;

namespace Plugins;

/// <summary>
/// Represents a plugin for purchasing products.
/// </summary>
internal sealed class DocumentPlugin
{
    [KernelFunction]
    /// This function is used to create an Word document.
    /// <param name="filename">The filename for the document</param>
    /// <param name="content">The text for the document</param>
    /// <returns>The path of the file</returns>
    public string CreateDocument(string name, string content)
    {
       // Get the path for file 
        string filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Purchases", name + ".docx");

        // Create a Word document 
       using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filepath, WordprocessingDocumentType.Document))
        {
            // Add a main document part
            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // Convert HTML to OpenXML elements
            HtmlConverter converter = new HtmlConverter(mainPart);
            Body body = mainPart.Document.Body ?? new Body();
            var paragraphs = converter.Parse(content);

            // Add the converted paragraphs to the document body
            foreach (var paragraph in paragraphs)
            {
                body.Append(paragraph);
            }

            // Save changes
            mainPart.Document.Save();
        }

        return filepath;
    }
}