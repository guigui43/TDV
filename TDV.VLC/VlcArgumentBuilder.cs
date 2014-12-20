﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TDV.VLC
{
    /// <summary>
    /// Base class for command line arguments
    /// </summary>
    public abstract class VlcArgumentBuilder
    {
        protected VlcArgumentBuilder()
        {
        }

        /// <summary>
        /// List of commands (with values).
        /// </summary>
        protected readonly Dictionary<string, string> commands = new Dictionary<string, string>();

        /// <summary>
        /// Sets (adds or updates) a command parameter.
        /// </summary>
        /// <param name="key">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        protected void SetString(string key, string value)
        {
            if (this.commands.ContainsKey(key))
            {
                this.commands[key] = value;
            }
            else
            {
                this.commands.Add(key, value);
            }
        }

        /// <summary>
        /// Sets (adds or updates) a boolean command parameter.
        /// </summary>
        /// <param name="value">The value (true or false).</param>
        /// <param name="trueValue">The argument string for "true".</param>
        /// <param name="falseValue">The argument string for "false".</param>
        protected void SetBoolean(bool value, string trueValue, string falseValue)
        {
            this.commands.Remove(trueValue);
            this.commands.Remove(falseValue);

            if (value)
            {
                this.commands.Add(trueValue, null);
            }
            else
            {
                this.commands.Add(falseValue, null);
            }
        }

        /// <summary>
        /// Build the complete arguments string.
        /// </summary>
        /// <returns></returns>
        public virtual string GetArgumentString()
        {
            StringBuilder result = new StringBuilder();

            foreach (var item in this.commands)
            {
                string value = item.Value;

                if (String.IsNullOrEmpty(value))
                {
                    result.AppendFormat(" {0}", item.Key);
                }
                else
                {
                    if (value.Contains(" "))
                    {
                        value = String.Concat("\"", value, "\"");
                    }

                    result.AppendFormat(" {0}={1}", item.Key, value);
                }
            }

            return result.ToString().Trim();
        }
    }
}
