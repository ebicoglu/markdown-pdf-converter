using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using Npgsql;

namespace AbpSupportQuestionsFetcher;


class Program
{

    //TODO UPDATE THESE OPTIONS
    //------------------------------------------------------------------------------------------------
    public static string OutputPdfPath = $"D:\\temp\\abp-support-{DateTime.Now:yyyy-MM-dd}.pdf";
    public static string ConnectionString = "Host=localhost;Database=AbpIoPlatform;Username=root;Password=root;Port=5432";
    public static int? MaxRecordCount = null;
    public static bool? OnlyAcceptedAnswers = true;
    //------------------------------------------------------------------------------------------------


    private static int RowCount = 0;
    private static ConverterProperties? _converterProperties;
    private static void InitializeFonts()
    {
        var fontProvider = new FontProvider();
        fontProvider.AddStandardPdfFonts();
        fontProvider.AddSystemFonts();

        _converterProperties = new ConverterProperties();
        _converterProperties.SetFontProvider(fontProvider);
    }

    private static void Main()
    {
        InitializeFonts();

        CreatePdf();

        Console.WriteLine($"PDF created successfully: {OutputPdfPath}");
        Console.ReadKey();
    }

    private static void CreatePdf()
    {
        using (var writer = new PdfWriter(OutputPdfPath))
        {
            using (var pdf = new PdfDocument(writer))
            {
                var document = new Document(pdf);

                AddFirstPage(document);

                using (var conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();

                    var query = BuildQuery();

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var lastQuestionId = string.Empty;
                            while (reader.Read())
                            {
                                ++RowCount;
                                lastQuestionId = ProcessLine(reader, lastQuestionId, document);
                            }
                        }
                    }
                }

                document.Close();
            }
        }
    }

    private static string BuildQuery()
    {
        var onlyAcceptedAnswers = OnlyAcceptedAnswers.HasValue && OnlyAcceptedAnswers.Value ? "AND a.\"IsAccepted\" = true" : "";
        var limitCount = MaxRecordCount.HasValue ? "LIMIT " + MaxRecordCount.Value : "";

        return @"

SELECT q.""Id"" AS QuestionId, 
	q.""Title"", 
	q.""Number"" AS QuestionNumber, 
	q.""Text"" AS QuestionText, 
	q.""CreationTime"" AS QuestionTime, 
	uCreator.""Name"" AS QuestionUser,
    a.""Text"" AS AnswerText, 
	a.""CreationTime"" AS AnswerTime,
	uAnswerer.""IsMember"" AS AnswerUserTeamMember,
	uAnswerer.""Name"" AS AnswerUser,
	a.""IsAccepted"" AnswerIsAccepted
FROM ""QaQuestions"" q
LEFT JOIN ""QaAnswers"" a ON q.""Id"" = a.""QuestionId""
LEFT JOIN ""QaUsers"" uCreator ON q.""CreatorId"" = uCreator.""Id""
LEFT JOIN ""QaUsers"" uAnswerer ON a.""CreatorId"" = uAnswerer.""Id""
WHERE 
	q.""IsDeleted"" = false AND 
	a.""IsDeleted"" = false " + onlyAcceptedAnswers +
@" ORDER BY q.""Number"", a.""CreationTime"" " + limitCount;
    }

    private static void AddFirstPage(Document document)
    {
        var s = "THIS DOCUMENT CONTAINS Q&A" + Environment.NewLine +
                "ON TECHNICAL ISSUES RELATED TO ABP " + Environment.NewLine +
                "IT IS RETRIEVED FROM https://abp.io/support/" + Environment.NewLine +
                $"CREATED ON {DateTime.Now:yyyy-MM-dd}";

        var p = new Paragraph(s);
        p.SetFontSize(16);
        p.SetFont(PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD));
        p.SetBorder(new SolidBorder(DeviceGray.BLACK, 2));
        p.SetPadding(20);

        // Get the page size
        var pageSize = document.GetPdfDocument().GetDefaultPageSize();

        // Set the paragraph position to the middle of the page
        p.SetFixedPosition(0, pageSize.GetHeight() / 2, pageSize.GetWidth());

        // Center align the text horizontally and vertically
        p.SetTextAlignment(TextAlignment.CENTER);
        p.SetVerticalAlignment(VerticalAlignment.MIDDLE);

        document.Add(p);
    }

    private static void AddHtmlToDocoument(Document document, string html)
    {
        IList<IElement> elements = HtmlConverter.ConvertToElements(html, _converterProperties);
        foreach (var element in elements)
        {
            document.Add((IBlockElement)element);
        }
    }

    private static string ProcessLine(NpgsqlDataReader reader, string lastQuestionId, Document document)
    {
        try
        {
            //SET VARIABLES
            string currentQuestionId = reader["QuestionId"].ToString();
            string questionTitle = reader["Title"].ToString();
            string questionNumber = reader["QuestionNumber"].ToString();
            string questionText = MdToHtmlConverter.Convert(reader["QuestionText"].ToString());
            string questionTime = Convert.ToDateTime(reader["QuestionTime"]).ToString("yyyy-MM-dd HH:mm");
            string answerText = MdToHtmlConverter.Convert(reader["AnswerText"] != DBNull.Value ? reader["AnswerText"].ToString() : null);
            string answerTime = reader["AnswerTime"] != DBNull.Value ? Convert.ToDateTime(reader["AnswerTime"]).ToString("yyyy-MM-dd HH:mm") : null;
            bool isAnswerAccepted = reader["AnswerIsAccepted"] != DBNull.Value && (bool)reader["AnswerIsAccepted"];
            bool isAnswerUserTeamMember = reader["AnswerUserTeamMember"] != DBNull.Value && (bool)reader["AnswerUserTeamMember"];
            //string answerUser = reader["AnswerUser"] != DBNull.Value ? reader["AnswerUser"].ToString() : null;
            //string questionUser = reader["QuestionUser"].ToString();

            Console.WriteLine(RowCount + ".) " + currentQuestionId);


            if (currentQuestionId != lastQuestionId)
            {
                //NEW QUESTION
                document.Add(new AreaBreak()); //add new document break;

                var questionLink = new Link(questionTitle, PdfAction.CreateURI($"https://abp.io/support/questions/{questionNumber}"));
                var paragraph = new Paragraph().Add(questionTitle).Add(questionLink)
                    .SetFontSize(16)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD))
                    .SetBorder(new OutsetBorder(DeviceGray.BLACK, 2))
                    .SetPadding(10)
                    .SetMarginBottom(5);

                document.Add(paragraph);

                // Add Question Metadata
                document.Add(new Paragraph($"The below question is asked on {questionTime}. Question number is " + questionNumber)
                    .SetFontSize(10)
                    .SetBorder(new DottedBorder(DeviceGray.GRAY, 1))
                    .SetPadding(5)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC))
                    .SetMarginBottom(5));

                AddHtmlToDocoument(document, questionText);
            }

            // Add Answer (if available)
            if (!string.IsNullOrEmpty(answerText))
            {
                // Answer Metadata
                var answerUserPlaceholder = isAnswerUserTeamMember ? "support team" : "customer";
                var answerAccepted = isAnswerAccepted ? " and this answer is accepted as the solution." : string.Empty;
                document.Add(new Paragraph($"The below answer of question {questionNumber} is given by the {answerUserPlaceholder} on {answerTime}{answerAccepted}")
                    .SetFontSize(10)
                    .SetBorder(new DottedBorder(DeviceGray.GRAY, 1))
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC))
                    .SetPadding(5)
                    .SetMarginBottom(5));

                // Answer Section
                AddHtmlToDocoument(document, answerText);
            }

            return currentQuestionId;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }


}