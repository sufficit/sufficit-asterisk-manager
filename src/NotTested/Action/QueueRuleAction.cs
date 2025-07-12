using Sufficit.Asterisk.Manager.Action;

namespace Sufficit.Asterisk.Manager.Action
{
    public class QueueRuleAction : ManagerAction
    {
        public QueueRuleAction()
        {
        }

        public QueueRuleAction(string rule)
        {
            Rule = rule;
        }

        public override string Action
        {
            get { return "QueueRule"; }
        }

        public string Rule { get; set; }
    }
}