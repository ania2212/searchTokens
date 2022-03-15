/*Suppose that one of the automotive vendors sent to you a firmware archive and asked you 
to find different security issues. We know that some of the vendors are using a special 
authentication tokens in the following format:
Starting with"<Tkn" then later 3 digits, 5 English capital letters followed by a "Tkn>". 
For example: <Tkn435JFIRKTkn>.
You should implement a function that receives a directory path on the disk and a path to a 
CSV file output. The function should find the above pattern in all the files under the 
directory tree of the given path and report the results into the output CSV in the following 
format:
o Path - The relative path of the found file to the given root path
o Token - The identified token string
o Occurrences - The number of occurrences of the Token inside the file Path
The results should be sorted by (Path, Occurrences, Token)
The function also needs to print to a json of the total finding of each token in all of the 
files. for example, if token <Tkn435JFIRKTkn> was found only in f1- 5 times and in f2 -
3 times it should print:
{"<Tkn435JFIRKTkn>": 8} */

using System;
using CsvHelper;
using System.IO;
using System.Globalization;
using System.Linq;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

string directory = "D:/c#/";
string csv = "D:/c#/tokens.csv";
string json = "D:/c#/tokens.json";

searchTokens(directory, csv);

void searchTokens(string inDirectory, string csvPath)
{
    string token_pattern = @"<Tkn\d\d\d[A-Z][A-Z][A-Z][A-Z][A-Z]Tkn>";

    string[] allDirectoryFiles = Directory.GetFiles(inDirectory, "", SearchOption.AllDirectories);


    //save tokens from files of the directory to a list
    List<structToken> all_tokens = new List<structToken>();

    for (int i = 0; i < allDirectoryFiles.Length; i++)
    {
        string fileContent = File.ReadAllText(allDirectoryFiles[i]);
        MatchCollection token_matches = Regex.Matches(fileContent, token_pattern);
        foreach (Match match in token_matches)
        {
            string[] stringContent = File.ReadAllLines(allDirectoryFiles[i]);

            for (int j = 0; j < stringContent.Length; j++)
            {
                if (stringContent[j].Contains($"{match.Value}"))
                {
                    int tokenIndex = all_tokens.IndexOf(new structToken { Path = allDirectoryFiles[i].Remove(0, inDirectory.Length), Token = match.Value });
                    if (tokenIndex != -1)
                    {
                        all_tokens.Add(new structToken { Token = match.Value, Path = allDirectoryFiles[i].Remove(0, inDirectory.Length), Occurences = all_tokens[tokenIndex].Occurences + 1 });
                        all_tokens.RemoveAt(tokenIndex);
                    }
                    else
                    {
                        all_tokens.Add(new structToken { Token = match.Value, Path = allDirectoryFiles[i].Remove(0, inDirectory.Length), Occurences = 1 });
                    }
                }
            }
        }

    }

    writeToCsv(all_tokens, csvPath);

    writeToJson(all_tokens, json);

}

void writeToCsv(in List<structToken> tokenLists, string csvPath)
{
    IList<structToken> csv_result = tokenLists.OrderBy(x => x.Path).ThenBy(x => x.Occurences).ThenBy(x => x.Token).ToList();
    using (var writer = new StreamWriter(csvPath))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(csv_result);
    }
}

void writeToJson(in List<structToken> tokenLists, string jsonPath)
{
    tokenLists.Sort();
    List<structToken> json_result = new List<structToken>();
    structToken equalTo = tokenLists[0];
    int countOccur = equalTo.Occurences;
    for (int i = 0; i < tokenLists.Count - 1; i++)
    {
        if (tokenLists[i + 1].Token == equalTo.Token)
        {
            countOccur = countOccur + tokenLists[i + 1].Occurences;
            if (i == tokenLists.Count - 2)
            {
                json_result.Add(new structToken { Token = equalTo.Token, Occurences = countOccur });
            }
        }
        else
        {
            json_result.Add(new structToken { Token = equalTo.Token, Occurences = countOccur });
            equalTo = tokenLists[i + 1];
            countOccur = tokenLists[i + 1].Occurences;
        }
    }
    using (FileStream fs = new FileStream(jsonPath, FileMode.OpenOrCreate))
    {
        JsonSerializer.SerializeAsync<IList<structToken>>(fs, json_result);
    }
}

struct structToken : IComparable
{
    [JsonIgnore]
    public string Path { get; set; }
    public string Token { get; set; }
    public int Occurences { get; set; }
    public int CompareTo(object? obj)
    {
        if (obj is structToken token) return Token.CompareTo(token.Token);
        else throw new ArgumentException("Uncorrect value");
    }

    public override bool Equals(object? obj)
    {

        if (obj is structToken token) return (Token == token.Token) && (Path == token.Path);
        return false;
    }
};
