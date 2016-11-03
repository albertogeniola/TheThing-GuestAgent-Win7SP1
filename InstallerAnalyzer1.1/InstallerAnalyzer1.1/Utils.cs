using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace InstallerAnalyzer1_Guest
{
    public class Utils
    {
        /// <summary>
        /// Will only serialize object properties properties.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="parent"></param>
        public static void DummySerialize(object o, XmlElement parent)
        {
            if (o == null || parent == null)
                return;

            // The stupid C# default XMLSerializer won't work without parameterless constructor. We need to implement
            // our Serializer using reflection
            var props = o.GetType().GetProperties();
            if (props == null)
                return;

            foreach (var p in props)
            {
                XmlElement e = parent.OwnerDocument.CreateElement(p.Name);
                object val = null;
                try
                {
                    val = p.GetValue(o, null);
                }
                catch (Exception ex)
                {
                    val = null;
                }

                if (val != null)
                {
                    // Check if this object is an instance of a primitive type. If not, apply recursion.
                    if (val.GetType().IsPrimitive || val.GetType() == typeof(string))
                    {
                        e.InnerText = val.ToString();
                    }
                    // Check if this is a collection of items
                    else if (val is System.Collections.IEnumerable)
                    {
                        foreach (var el in val as System.Collections.IEnumerable)
                        {
                            XmlElement litem = parent.OwnerDocument.CreateElement(p.Name + "_Item");
                            DummySerialize(el, litem);
                            e.AppendChild(litem);
                        }
                    }
                    // Otherwise apply recursion
                    else {
                        DummySerialize(val, e);
                    }
                }
                else
                    e.InnerText = "";

                parent.AppendChild(e);
            }
        }

        public struct Hashes
        {
            public string sha1;
            public string md5;
        }

        public static Hashes CalculateHash(string filePath)
        {
            Hashes res = new Hashes() { sha1 = null, md5 = null };
            try
            {
                /*
                using(var sha1 = SHA1.Create())
                using (var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    md5.
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }*/

                using (var md5 = MD5Cng.Create()) // Or MD5Cng.Create
                using (var sha1 = SHA1Cng.Create()) // Or SHA1Cng.Create
                using (var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        md5.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        sha1.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    }
                    // We have to call TransformFinalBlock, but we don't have any
                    // more data - just provide 0 bytes.
                    md5.TransformFinalBlock(buffer, 0, 0);
                    sha1.TransformFinalBlock(buffer, 0, 0);

                    res.md5 = BitConverter.ToString(md5.Hash).Replace("-", string.Empty);
                    res.sha1 = BitConverter.ToString(sha1.Hash).Replace("-", string.Empty);
                }

            }
            catch (Exception e)
            {
                // This should never happen at this time, but we don't want to stuck the process if it happens.
            }
            return res;
        }
    }
}
