// Framework.cs
// Simple stack-based testing framework for Pointy

// Copyright (c) 2010 Patrick Stein
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using System.Collections.Generic;

//FIXME - Seriously lacking in documentation

namespace PointyTests
{
    /// <summary>
    /// This is a little test framework I developed due to disliking things about other testing frameworks.  It's
    /// not really intended to be portable or anything, but rather to be used exclusively with Pointy.
    /// 
    /// The core principle is nested stacks.
    /// </summary>
    static class Tests
    {
        class Test
        {
            bool HasChildren;

            string _Name;
            List<Test> Children;
            Test _Parent;
            bool _Passed;
            string _Details;

            public string Name
            {
                get
                {
                    return _Name;
                }
            }
            public Test Parent
            {
                get
                {
                    return _Parent;
                }
            }
            public bool Passed
            {
                get
                {
                    if (!HasChildren)
                        return _Passed;

                    foreach (Test child in Children)
                        if (!child.Passed)
                            return false;
                    return true;
                }
            }
            public string Details
            {
                get
                {
                    return _Details;
                }
                set
                {
                    _Details = value;
                }
            }

            public Test(string name, Test parent)
            {
                _Name = name;
                _Parent = parent;

                HasChildren = true;
                Children = new List<Test>();
            }
            public Test(string name, Test parent, bool passed)
            {
                HasChildren = false;
                _Passed = passed;
                _Parent = parent;
                _Name = name;
            }
            public void AddChild(Test test)
            {
                if (!HasChildren)
                    throw new Exception("This test does not support children (call the constructor without the 'passed' argument");

                Children.Add(test);
            }

            public override string ToString()
            {
                if (Passed)
                {
                    return _Name + ": PASS\n";
                }
                else
                {
                    StringBuilder bob = new StringBuilder();
                    bob.Append(_Name + ": FAIL\n");
                    if (_Details != null)
                        bob.Append("  Details: " + _Details + "\n");

                    if (HasChildren)
                    {
                        foreach (Test t in Children)
                        {
                            string[] lines = t.ToString().Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < lines.Length; i++)
                                bob.Append("  " + lines[i] + "\n"); //indentation action
                            bob.Append("\n");
                        }
                    }

                    return bob.ToString();
                }

            }
            public void OutputConsole()
            {
                OutputConsole("");
            }
            public void OutputConsole(string indent)
            {
                Console.Write(indent + _Name + ": ");
                if (Passed)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("PASS");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("FAIL");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    if (_Details != null)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(indent + "  Details: " + _Details);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    if (HasChildren)
                    {
                        foreach (Test t in Children)
                        {
                            t.OutputConsole(indent + "  ");
                        }
                    }
                }
            }
        }
        class ExpectTest : Test
        {
            public ExpectTest(Test parent, object expect, object check) : this(parent, expect, check, "Expect \"{0}\"")
            {

            }
            public ExpectTest(Test parent, object expect, object check, string message) : base(String.Format(message, expect), parent, expect == null ? check == null : expect.Equals(check))
            {
                this.Details = string.Format("Expected {0}, got {1}", expect, check);
            }
        }
        

        static Test Current = new Test("Tests", null);

        public static void PushTest(string desc)
        {
            Test test = new Test(desc, Current);
            Current.AddChild(test);
            Current = test;
        }
        public static void PopTest()
        {
            Current = Current.Parent;
        }

        // Testing methods

        /// <summary>
        /// Test that the given expected object matches the given object
        /// </summary>
        /// <param name="expect">Object expected</param>
        /// <param name="check">Object to compare to expected</param>
        /// <returns>Boolean indicating if the test passed</returns>
        public static bool Expect(object expect, object check)
        {
            Current.AddChild(new ExpectTest(Current, expect, check));

            if (expect == null)
                return check == null;
            else
                return expect.Equals(check);
        }
        /// <summary>
        /// Test that the given expected object matches the given object
        /// </summary>
        /// <param name="expect">Object expected</param>
        /// <param name="check">Object to compare to expected</param>
        /// <param name="message">Message describing test</param>
        /// <returns>Boolean indicating if the test passed</returns>
        public static bool Expect(object expect, object check, string message)
        {
            Current.AddChild(new ExpectTest(Current, expect, check, message));

            if (expect == null)
                return check == null;
            else
                return expect.Equals(check);
        }

        /// <summary>
        /// Tests that two dictionaries are equal, looking for missing keys, unequal values, and extra keys.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expect"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static bool DictCompare<TKey, TValue>(Dictionary<TKey, TValue> expect, Dictionary<TKey, TValue> check)
        {
            bool passed = true;

            //look for missing/unequal keys
            foreach (TKey key in expect.Keys)
            {
                //make sure the key exists
                if (!check.ContainsKey(key))
                    passed &= Result(false, string.Format("Contains key '{0}'", key));
                //compare values
                else
                    passed &= Expect(expect[key], check[key], string.Format("Value for key '{0}' is '{1}'", key, expect[key]));
            }

            //look for extra keys in the check that shouldn't be there
            foreach (TKey key in check.Keys)
            {
                //fail if the key isn't present in expect
                if (!expect.ContainsKey(key))
                    passed &= Result(false, string.Format("Extra key '{0}' (value '{1}')", key, check[key]));
            }

            return passed;
        }

        /// <summary>
        /// A test with a given description has the given result
        /// </summary>
        /// <param name="result">Result of the test</param>
        /// <param name="message">Test description</param>
        /// <returns>Result argument</returns>
        public static bool Result(bool result, string message)
        {
            Current.AddChild(new Test(message, Current, result));
            return result;
        }
        /// <summary>
        /// A test with a given description has the given result
        /// </summary>
        /// <param name="result">Result of the test</param>
        /// <param name="message">Test description</param>
        /// <param name="details">Extra information about the test</param>
        /// <returns>Result argument</returns>
        public static bool Result(bool result, string message, string details)
        {
            Test t = new Test(message, Current, result);
            t.Details = details;
            Current.AddChild(t);
            return result;
        }

        public new static string ToString()
        {
            Test root = Current;
            while (root.Parent != null)
                root = root.Parent;
            return root.ToString();
        }

        public static void OutputConsole()
        {
            //find the root test
            Test root = Current;
            while (root.Parent != null)
                root = root.Parent;
            
            //and print it to the console
            root.OutputConsole();

            Console.WriteLine();
        }
    }
}