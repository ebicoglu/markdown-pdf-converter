namespace AbpDocsMd2PdfConverter;

public class ParameterCombinator
{
    public static Dictionary<string, string>[] Generate(Dictionary<string, string[]> parameters)
    {
        var keys = parameters.Keys.ToList();
        var combinations = new List<Dictionary<string, string>>();
        var current = new Dictionary<string, string>();

        void Generate(int index)
        {
            if (index == keys.Count)
            {
                //combinations.Add(new Dictionary<string, string>(current));
                return;
            }

            var key = keys[index];
            foreach (var value in parameters[key])
            {
                current[key] = value;
                Generate(index + 1);
            }
        }

        Generate(0);
        return combinations.ToArray();
    }
}