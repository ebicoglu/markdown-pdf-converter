using Scriban.Runtime;
using Scriban;

namespace AbpDocsMd2PdfConverter;

public static class ScribanRenderer
{

    public static async Task<string> Render(
        string template,
        Dictionary<string, string>? parameters,
        Dictionary<string, string>? displayParameters)
    {
        var scribanTemplate = Template.Parse(template);
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        if (parameters != null && parameters.Any())
        {
            foreach (var param in parameters)
            {
                scriptObject.Add(param.Key, param.Value);
            }
        }

        if (displayParameters != null && displayParameters.Any())
        {
            foreach (var displayParam in displayParameters)
            {
                scriptObject.Add(displayParam.Key + "_Value", displayParam.Value);
            }
        }

        context.PushGlobal(scriptObject);
        return await scribanTemplate.RenderAsync(context);
    }

}