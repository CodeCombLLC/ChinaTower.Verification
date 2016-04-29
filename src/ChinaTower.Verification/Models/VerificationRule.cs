using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.WebUtilities;
using Microsoft.AspNet.Mvc.Rendering;
using ChinaTower.Verification.Models.Infrastructures;
using CodeComb.Data.Verification;
using CodeComb.Data.Verification.EntityFramework;

namespace ChinaTower.Verification.Models
{
    public class VerificationRule
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Alias { get; set; }

        public FormType Type { get; set; }

        [ForeignKey("Rule")]
        public Guid RuleId { get; set; }

        public virtual DataVerificationRule Rule { get; set; }

        public HtmlString ToHtmlString(bool Editing = false)
        {
            return new HtmlString(RenderRules(Rule.RuleObject, Type, Editing));
        }

        public static string RenderRules(ICollection<Rule> rules, FormType type, bool Editing = false)
        {
            var str = new StringBuilder("<ul>");
            if (rules.Count > 0)
            {
                foreach (var a in rules)
                {
                    str.AppendLine($"<li data-type=\"{a.Type}\" data-expression=\"{a.Expression}\" data-argument-index=\"{a.ArgumentIndex}\">");
                    if (a.Type == RuleType.And)
                    {
                        str.AppendLine($"<span>满足下面的全部 {a.NestedRules.Count} 个子条件</span>");
                    }
                    else if (a.Type == RuleType.Or)
                    {
                        str.AppendLine($"<span>满足下面的 {a.NestedRules.Count} 个子条件中的任意一个</span>");
                    }
                    else if (a.Type == RuleType.Not)
                    {
                        str.AppendLine($"<span>不可符合下面的 {a.NestedRules.Count} 个子条件中的任意一个</span>");
                    }
                    else if (a.Type == RuleType.Gt)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } ＞ { a.Expression }</span>");
                    }
                    else if (a.Type == RuleType.Gte)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } ≥ {a.Expression}</span>");
                    }
                    else if (a.Type == RuleType.Lt)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } ＜ {a.Expression}</span>");
                    }
                    else if (a.Type == RuleType.Lte)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } ≤ {a.Expression}</span>");
                    }
                    else if (a.Type == RuleType.Equal)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } = {a.Expression}</span>");
                    }
                    else if (a.Type == RuleType.NotEqual)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } ≠ {a.Expression}</span>");
                    }
                    else if (a.Type == RuleType.Empty)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } 必须为空</span>");
                    }
                    else if (a.Type == RuleType.NotEmpty)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } 不能为空</span>");
                    }
                    else if (a.Type == RuleType.Regex)
                    {
                        str.AppendLine($"<span>{ GetHeaderOfIndex(type, a.ArgumentIndex) } 满足正则表达式 { a.Expression }</span>");
                    }
                    if (Editing)
                    {
                        str.AppendLine($"<a href=\"javascript:;\" onclick=\"removeRule(this)\">删除</a>");
                    }
                    if (new RuleType[] { RuleType.And, RuleType.Or, RuleType.Not }.Contains(a.Type))
                    {
                        str.AppendLine(RenderRules(a.NestedRules, type, Editing));
                    }
                    str.AppendLine("</li>");
                }
            }
            
            if (Editing)
            {
                str.AppendLine($"<li class=\"li-insert\"><a onclick=\"insertRule(this)\" href=\"javascript:;\">添加</a></li>");
            }
            str.AppendLine("</ul>");
            return str.ToString();
        }

        public static string GetHeaderOfIndex(FormType type, int index)
        {
            return Hash.Headers[type][index];
        }
    }
}
