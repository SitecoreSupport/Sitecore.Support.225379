using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;

namespace Sitecore.Support.Shell.Framework.Commands
{
    /// <summary>
    /// Represents the Create command.
    /// </summary>
    [Serializable]
    public class Create : Command
    {
        /// <summary>
        /// Executes the command in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Execute(CommandContext context)
        {
            if (context.Items.Length != 1)
            {
                return;
            }
            Item item = context.Items[0];
            if (!item.Access.CanCreate())
            {
                SheerResponse.Alert("You do not have permission to create a new item here.", new string[0]);
                return;
            }
            string @string = StringUtil.GetString(new string[]
            {
                context.Parameters["template"]
            });
            string string2 = StringUtil.GetString(new string[]
            {
                context.Parameters["master"]
            });
            string string3 = StringUtil.GetString(new string[]
            {
                context.Parameters["prompt"]
            });
            NameValueCollection nameValueCollection = new NameValueCollection();
            BranchItem branchItem = null;
            TemplateItem templateItem = null;
            if (string2.Length > 0)
            {
                branchItem = Context.ContentDatabase.Branches[string2];
                Error.Assert(branchItem != null, "Master \"" + string2 + "\" not found.");
            }
            else if (@string.Length > 0)
            {
                templateItem = Context.ContentDatabase.Templates[@string, item.Language];
                Error.Assert(templateItem != null, "Template \"" + @string + "\" not found.");
            }
            if (branchItem != null || templateItem != null)
            {
                nameValueCollection["prompt"] = string3;
                nameValueCollection["id"] = item.ID.ToString();
                nameValueCollection["database"] = item.Database.Name;
                nameValueCollection["language"] = item.Language.Name; //added language parameter to the nameValueCollection
                if (branchItem != null)
                {
                    nameValueCollection["master"] = branchItem.ID.ToString();
                }
                if (templateItem != null)
                {
                    nameValueCollection["template"] = templateItem.ID.ToString();
                }
                Context.ClientPage.Start(this, "Run", nameValueCollection);
            }
        }

        /// <summary>
        /// Queries the state of the command.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The state of the command.</returns>
        public override CommandState QueryState(CommandContext context)
        {
            if (context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            if (!context.Items[0].Access.CanDelete())
            {
                return CommandState.Disabled;
            }
            return base.QueryState(context);
        }

        /// <summary>
        /// Runs the specified args.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected void Run(ClientPipelineArgs args)
        {
            string @string = StringUtil.GetString(new string[]
            {
                args.Parameters["master"]
            });
            string string2 = StringUtil.GetString(new string[]
            {
                args.Parameters["template"]
            });
            string string3 = StringUtil.GetString(new string[]
            {
                args.Parameters["database"]
            });
            Database database = Factory.GetDatabase(string3);
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    
                    Item item = database.Items[args.Parameters["id"], Sitecore.Data.Managers.LanguageManager.GetLanguage(args.Parameters["language"])]; 
                    if (item == null)
                    {
                        Context.ClientPage.ClientResponse.ShowError("Parent item not found.", "");
                        args.AbortPipeline();
                        return;
                    }
                    if (@string.Length > 0)
                    {
                        BranchItem branch = database.Branches[@string, item.Language];
                        Item item2 = Context.Workflow.AddItem(args.Result, branch, item);
                        Log.Audit(this, "Add item : {0}", new string[]
                        {
                            AuditFormatter.FormatItem(item2)
                        });
                        return;
                    }

                    TemplateItem templateItem = database.Templates[string2, item.Language];
                    Item item3 = templateItem.AddTo(item, args.Result);
                    Log.Audit(this, "Add item : {0}", new string[]
                    {
                        AuditFormatter.FormatItem(item3)
                    });
                    return;
                }
            }
            else
            {
                string defaultValue = string.Empty;
                string string4 = StringUtil.GetString(new string[]
                {
                    args.Parameters["prompt"],
                    "Enter a name for the new item:"
                });
                if (@string.Length > 0)
                {
                    BranchItem branchItem = database.Branches[@string, Sitecore.Data.Managers.LanguageManager.GetLanguage(args.Parameters["language"])]; 
                    defaultValue = branchItem.Name;
                }
                else
                {
                    TemplateItem templateItem2 = database.Templates[string2, Sitecore.Data.Managers.LanguageManager.GetLanguage(args.Parameters["language"])];
                    defaultValue = templateItem2.Name;
                }
                Context.ClientPage.ClientResponse.Input(string4, defaultValue, Settings.ItemNameValidation, "'$Input' is not a valid name.", Settings.MaxItemNameLength);
                args.WaitForPostBack();
            }
        }
    }
}
