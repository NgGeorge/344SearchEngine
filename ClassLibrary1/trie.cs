using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace ClassLibrary
{
    // This class creates a trie data structure that stores the characters of the inputted strings
    public class trie
    {

        private node root;
        public node currentNode;
        public int count = 0;
        public string lastLine = "";

        public trie() {
            root = new node();
        }

        //Adds a title to the trie
        public void AddTitle(string title) {
            lastLine = title;
            count++;
            currentNode = root;
            for (var i = 0; i < title.Length; i++) {
                if (i != (title.Length - 1))
                {
                    if (currentNode.isList)
                    {
                        AddToList(currentNode, title);
                        break;
                    } else { 
                    currentNode = currentNode.GetOrAddChild(title[i], false);
                    }
                } else {
                    if ( currentNode.isList )
                    {
                        AddToList(currentNode, title);
                        break;
                    } else
                    {
                        currentNode = currentNode.GetOrAddChild(title[i], true);
                    }
                }
            }
        }

        // Searches for up to 10 suggestions in the trie
        public List<string> SearchForPrefix(string title)
        {
            currentNode = root;
            List<string> suggestionList = new List<string>();
            foreach (char c in title)
            {
                if ((currentNode.children != null) && currentNode.children.ContainsKey(c))
                {
                        currentNode = currentNode.children[c];
                }
                else
                {
                    foreach (string name in currentNode.titles)
                    {
                        if (suggestionList.Count < 10)
                        {
                            if (name.Contains(title) && !suggestionList.Contains(name))
                            {
                                suggestionList.Add(name);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (suggestionList.Count == 0)
                    {
                        suggestionList.Add("No Results");
                    }
                    return suggestionList;
                }
            }
            if (title.Length > 1)
            {
                title = title.Substring(0, title.Length - 1);
            }
            else
            {
                title = "";
            }
            suggestionList = SearchHelper(title, currentNode, suggestionList);
            return suggestionList;
        }

        // This method traverses the trie
        private List<string> SearchHelper(string prefix, node newNode, List<string> suggestionList)
        {

            if (suggestionList.Count < 10)
            {
                if (newNode.titleEnd == true)
                {
                    if (!suggestionList.Contains(prefix + newNode.value))
                    {
                        suggestionList.Add(prefix + newNode.value);
                    }
                }
                if (newNode.isList)
                {
                    foreach (string title in newNode.titles)
                    {
                        if (suggestionList.Count < 10)
                        {
                            if (!suggestionList.Contains(title))
                            {
                                suggestionList.Add(title);
                            }
                        }
                        else
                        {
                            return suggestionList;
                        }
                    }
                }
                else
                {
                    foreach (node child in newNode.GetChildren())
                    {
                        suggestionList = SearchHelper(prefix + newNode.value, child, suggestionList);
                    }
                }
                return suggestionList;
            }
            else
            {
                return suggestionList;
            }
        }

        // Adds a title to a list-node, and converts a list-node to a regular node if list > 25
        private void AddToList(node currentNode, string title)
        {
            currentNode.titles.Add(title);
            if (currentNode.titles.Count > 25)
            {
                currentNode.isList = false;
                foreach (string name in currentNode.titles)
                {
                    count--;
                    AddTitle(name);
                }
                currentNode.titles.Clear();
            }
        }

    }
}