using System;
using System.Collections.Generic;

namespace ClassLibrary
{
    // This class creates a node for use in the trie
    public class node
    {
        public bool isList;
        public List<string> titles;
        public char value { get; private set; }
        public bool titleEnd { get; private set; }
        public Dictionary<char, node> children { get; private set; }

        public node()
        {
            isList = true;
            titles = new List<string>();
        }

        public node(char inputChar, bool title)
        {
            isList = true;
            value = inputChar;
            titleEnd = title;
            titles = new List<string>();
        }

        // This method returns a child if it already exists with the given key, or creates and returns a new node if not.
        public node GetOrAddChild(char inputChar, bool title)
        {
            if (children == null)
            {
                children = new Dictionary<char, node>();
            }
            if (!children.ContainsKey(inputChar))
            {
                children.Add(inputChar, new node(inputChar, title));
                return children[inputChar];
            }
            else
            {
                return children[inputChar];
            }
        }

        // This method returns the children 
        public List<node> GetChildren()
        {
            return new List<node>(children.Values);
        }

    }
}