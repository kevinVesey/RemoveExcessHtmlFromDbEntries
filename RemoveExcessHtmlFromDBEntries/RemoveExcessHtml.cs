using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace RemoveExcessHtmlFromDBEntries
{
    public class RemoveExcessHtml
    {
        public DataTable RemoveHtml(DataTable dataForUpdate)
        {
            const string newValueColumn = "UpdatedValue";
            string primaryKeyColumn = dataForUpdate.Columns[0].ColumnName;
            string htmlForUpdateColumn = dataForUpdate.Columns[1].ColumnName;

            DataTable updatedDataTable = CreateUpdatedDataTable(primaryKeyColumn, newValueColumn);
            

            foreach (DataRow htmlRow in dataForUpdate.Rows)
            {
                HtmlNode.ElementsFlags.Remove("p");
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlRow.Field<string>(htmlForUpdateColumn));
                if (IsRowForHtmlEdit(htmlDoc))
                {
                    DataRow updateTableRow = updatedDataTable.NewRow();
                    RemoveEmptyTags(htmlDoc.DocumentNode);
                    htmlDoc.DocumentNode.InnerHtml = ReplaceIncorrectTags(htmlDoc.DocumentNode.OuterHtml);
                    RemoveDuplicateStyles(htmlDoc.DocumentNode);
                    RemoveDuplicateTags(htmlDoc.DocumentNode);
                    updateTableRow[primaryKeyColumn] = htmlRow.Field<int>(primaryKeyColumn);
                    string newValue = htmlDoc.DocumentNode.OuterHtml;
                    if (htmlRow.Field<string>(htmlForUpdateColumn) != newValue)
                    {
                       newValue = newValue.Replace("'", "''");
                        updateTableRow[newValueColumn] = newValue;
                        updatedDataTable.Rows.Add(updateTableRow);
                    }                       
                }
            }
            return updatedDataTable;
        }
        private void RemoveEmptyTags(HtmlNode htmlDocNodes)
        {
            if (string.IsNullOrEmpty(htmlDocNodes.InnerText) && htmlDocNodes.Name != "br")
                htmlDocNodes.Remove();
            else
            {
                for (var i = htmlDocNodes.ChildNodes.Count - 1; i >= 0; i--)
                {
                    RemoveEmptyTags(htmlDocNodes.ChildNodes[i]);
                }
            }
        }
        private void RemoveDuplicateStyles(HtmlNode htmlDocNodes)
        {
            var allNodesCollection = htmlDocNodes.Descendants().ToList();
            const string styleAttribute = "style";
            var duplicateStyles = allNodesCollection.GroupBy(x => x.InnerText + x.Attributes[styleAttribute]);
            foreach (var duplicateStyleNode in duplicateStyles.Reverse())
            {
                for (var i = duplicateStyleNode.Count() - 2; i >= 0; i--)
                {
                    if (duplicateStyleNode.ElementAt(i).NodeType != HtmlNodeType.Element ||
                        !duplicateStyleNode.ElementAt(i).HasAttributes) continue;

                    var node = htmlDocNodes;
                    string xPath = duplicateStyleNode.ElementAt(i).XPath;
                    int elementsCount = GetXpathElementsCount(xPath);
                    if (elementsCount > 511)
                    {
                        List<string> splitXpaths = SplitXpath(xPath);
                        bool isfirstNode = true;
                        foreach (var xPathPart in splitXpaths)
                        {
                            if (isfirstNode)
                            {
                                node = htmlDocNodes.SelectSingleNode(xPathPart);
                                isfirstNode = false;
                            }
                            else
                            {
                                if (node.FirstChild != null)
                                    node = node.FirstChild;
                            }
                        }
                        node.Attributes[styleAttribute]?.Remove();
                    }
                    else
                    {
                        node = htmlDocNodes.SelectSingleNode(xPath);
                        node.Attributes[styleAttribute]?.Remove();
                    }
                }
            }
        }

        private void RemoveDuplicateTags(HtmlNode htmlDocNodes)
        {
            const string styleAttribute = "style";        
            bool isNodeDeleted = false;
            restart:
            var allNodesCollection = htmlDocNodes.Descendants().ToList();
            var duplicateNodes = allNodesCollection.GroupBy(x => x.Name + x.InnerText + x.GetAttributeValue(styleAttribute,""));           
            foreach (var duplicateChild in duplicateNodes.Reverse())
            {               
                for(var i = duplicateChild.Count() -2 ; i >=0; i--)
                {
                    if (duplicateChild.ElementAt(i).NodeType != HtmlNodeType.Element && duplicateChild.ElementAt(i).Name != "br") continue;

                    var nodeForRemoval = htmlDocNodes;
                    string xPath = duplicateChild.ElementAt(i).XPath;
                    int elementsCount = GetXpathElementsCount(xPath);
                    if (elementsCount > 511)
                    {
                        List<string> splitXpaths = SplitXpath(xPath);
                        bool isfirstNode = true;
                        foreach (var xPathPart in splitXpaths)
                        {
                            if (isfirstNode)
                            {
                                nodeForRemoval = htmlDocNodes.SelectSingleNode(xPathPart);
                                isfirstNode = false;
                            }
                            else
                            {
                                if(nodeForRemoval.FirstChild!=null)
                                    nodeForRemoval = nodeForRemoval.FirstChild;
                            }
                        }
                        if (nodeForRemoval.ChildNodes == null)
                        {
                            nodeForRemoval.Remove();
                            isNodeDeleted = true;
                        }
                        else if (nodeForRemoval.ParentNode != null)
                        {
                            nodeForRemoval.ParentNode.RemoveChild(nodeForRemoval, true);
                            isNodeDeleted = true;
                        }                      
                    }
                    else
                    {
                        nodeForRemoval = htmlDocNodes.SelectSingleNode(xPath);
                        if (nodeForRemoval == null) continue;
                        if (nodeForRemoval.ChildNodes == null)
                        {
                            nodeForRemoval.Remove();
                            isNodeDeleted = true;
                        }                           
                        else if (nodeForRemoval.ParentNode != null)
                        {
                            nodeForRemoval.ParentNode.RemoveChild(nodeForRemoval, true);
                            isNodeDeleted = true;
                        }                       
                    }                   
                }
                if(isNodeDeleted)
                {
                    isNodeDeleted = false;
                    goto restart;
                }
            }
        }     
        private string ReplaceIncorrectTags(string html)
        {
            Regex regex = new Regex(@"(?<=<\/)([a-zA-Z]*:)");
                html = regex.Replace(html,"");
            regex = new Regex(@"((?<=<)[a-zA-Z]*:)");
                html = regex.Replace(html,"");
           return html;
        }
        private bool IsRowForHtmlEdit(HtmlDocument htmlDoc)
        {
            const string tableNode = "table";
            if (htmlDoc.DocumentNode.InnerHtml == htmlDoc.DocumentNode.InnerText)
                return false;
            return htmlDoc.DocumentNode.FirstChild.Name != tableNode;
        }
        private DataTable CreateUpdatedDataTable(string column1, string column2)
        {
            DataTable updatedData = new DataTable();
            updatedData.Columns.Add(column1, typeof(int));
            updatedData.Columns.Add(column2, typeof(string));
            return updatedData;
        }
        private int GetXpathElementsCount(string xPath)
        {
            int elementCount = 0;
            foreach (var c in xPath)
            {
                if (c == '/') elementCount++;
            }
            return elementCount;
        }
        private List<string> SplitXpath(string xPath)
        {
            const int occuranceToSplitAt = 511;
            List<string> xPathElements = new List<string>();
            while (GetXpathElementsCount(xPath) >= 511)
            {
                int count = 0;
                for (int i = 0; i < xPath.Length; i++)
                {
                    if (xPath[i] == '/')
                        count++;
                    if (count != occuranceToSplitAt) continue;

                    xPathElements.Add(xPath.Substring(0, i));
                    int remainingLenght = xPath.Length - i;
                    xPath = xPath.Substring(i, remainingLenght);
                }
            }
            foreach (string singleElement in xPath.Split('/'))
            {
                if(!string.IsNullOrEmpty(singleElement))
                    xPathElements.Add($"/{singleElement}");
            }
            return xPathElements;
        }
    }
}
