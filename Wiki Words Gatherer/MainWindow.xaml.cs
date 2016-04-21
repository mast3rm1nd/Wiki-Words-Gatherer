using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Wiki_Words_Gatherer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int minLength = 0;
        static int maxLength = int.MaxValue;


        static int parsingDepth = 0;

        static string filename = @"Wordlist.txt";


        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_StartStop_Click(object sender, RoutedEventArgs e)
        {
            var positiveDigitsRegex = @"^\d+$";
            var wikiArticleUrlRegex = @"^https*://\w+.wikipedia.org/wiki/.+";
            var errorMessage = "";

            if (!Regex.IsMatch(TextBox_StartingPage.Text, wikiArticleUrlRegex))
                errorMessage += "Format of Wiki article URL is incorrect." + "\n";

            if (!Regex.IsMatch(TextBox_ParsingDepth.Text, positiveDigitsRegex))
                errorMessage += "Parsing depth must be positive integer." + "\n";

            if (!Regex.IsMatch(TextBox_MinLength.Text, positiveDigitsRegex))
                errorMessage += "Minimum length must be positive integer." + "\n";

            if (!Regex.IsMatch(TextBox_MaxLength.Text, positiveDigitsRegex) && TextBox_MaxLength.Text != "")
                errorMessage += "Maximum length must be positive integer or empty string." + "\n";

            minLength = int.Parse(TextBox_MinLength.Text);

            if(TextBox_MaxLength.Text == "")
                maxLength = int.MaxValue;
            else
                maxLength = int.Parse(TextBox_MaxLength.Text);

            if (maxLength < minLength)
                errorMessage += "Maximum length can't be lesser than minimun length." + "\n";


            parsingDepth = int.Parse(TextBox_ParsingDepth.Text);


            if(errorMessage != "")
            {
                MessageBox.Show(errorMessage, "Invalid inputs.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            filename = GetArticleName(TextBox_StartingPage.Text) + ".txt";

            downloadingUrls.Clear();
            allPagesHTML.Clear();
            uniqueWords.Clear();


            await GatherUniqueWords(TextBox_StartingPage.Text, 0, parsingDepth);

            var deduped = uniqueWords.Distinct().ToList();
            deduped.Sort();
            File.WriteAllText(filename, "");
            File.WriteAllLines(filename, deduped);
            MessageBox.Show("Saved to " + filename, "Done.");
        }

        public static void WhenDone()
        {
            var deduped = uniqueWords.Distinct().ToList();
            deduped.Sort();
            File.WriteAllText(filename, "");
            File.WriteAllLines(filename, deduped);
            MessageBox.Show("Saved to " + filename, "Done.");
        }


        static string GetArticleName(string url)
        {
            for(var pos = url.Length - 1; ; pos--)
            {
                if (url[pos] == '/')
                    return url.Substring(pos + 1, url.Length - pos - 1);
            }
        }


        static string[] GetAllWordsFromText(string text, int minLength, int maxLength)
        {
            List<string> words = new List<string>();

            int copy_StartPos = 0;
            bool is_InWord = false;

            for(int position = 0; position < text.Length; position++)
            {
                if(is_InWord && !char.IsLetter(text[position]))//были в слове и вышли за границы
                {
                    is_InWord = false;
                    var wordLength = position - copy_StartPos;

                    if(wordLength >= minLength)
                    if(wordLength <= maxLength)
                    {
                        var found_word = text.Substring(copy_StartPos, wordLength);

                        words.Add(found_word);
                    }
                }
                else if(!is_InWord && char.IsLetter(text[position]))//были не в слове, попали в слово
                {
                    is_InWord = true;

                    copy_StartPos = position;
                }                    
            }

            return words.ToArray();
        }


        static string[] GetAllWikiArticlesLinksFromPage(string pageHTML)
        {
            List<string> foundLinks = new List<string>();
            string wikiDomain = "";

            MatchCollection matchesWikiLinks = Regex.Matches(pageHTML, "/wiki/(.+?)\"", RegexOptions.Multiline);
            MatchCollection matchWikiDomain = Regex.Matches(pageHTML, "html lang=\"(.+?)\"", RegexOptions.Multiline);

            foreach (Match m in matchWikiDomain)
            {
                wikiDomain = m.Groups[1].Value;
            } 

            foreach (Match m in matchesWikiLinks)
            {
                if(m.Groups[1].Value.Split(':').Count() == 1)//только если ":" один
                    foundLinks.Add(String.Format("https://{0}.wikipedia.org/wiki/{1}", wikiDomain, m.Groups[1].Value));
            }

            return foundLinks.ToArray();
        }



        static List<string> downloadingUrls = new List<string>();
        static List<string> allPagesHTML = new List<string>();
        static List<string> uniqueWords = new List<string>();
        public static async Task<bool> GatherUniqueWords(string uri, int currentDepth, int maxDepth)//скачивание всех уникальных статей чтобы потом
        {                                                                                        //записать в файл только уникальные слова
            if (downloadingUrls.Contains(uri))                                                   //но в то же время запись в него промежуточных рез-тов
                return false;                         

            if (currentDepth > maxDepth)
                return false;
            
            downloadingUrls.Add(uri);

            var currentUriHTML = await DownloadHelper.DownloadHtmlAsync(uri);

            var articlesUrlsOneLevelBelow = GetAllWikiArticlesLinksFromPage(currentUriHTML);
            var currentWords = GetAllWordsFromText(currentUriHTML, minLength, maxLength);

            foreach(string word in currentWords)
            {
                if (!uniqueWords.Contains(word))
                    uniqueWords.Add(word);
            }

            File.AppendAllLines(filename, currentWords);


            var taskDownloading = new List<Task<bool>>();
            foreach (string levelBelowUrl in articlesUrlsOneLevelBelow)
            {
                taskDownloading.Add(GatherUniqueWords(levelBelowUrl, currentDepth + 1, maxDepth));
            }
            await Task.WhenAll(taskDownloading);

            return true;
        }
    }
}
