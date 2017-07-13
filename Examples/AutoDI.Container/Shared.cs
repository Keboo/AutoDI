using System.Collections.Generic;

namespace AutoDI.Container.Examples
{
    public interface IQuoteService
    {
        IEnumerable<Quote> GetQuotes();
    }

    public class PrincessBrideQuoteService : IQuoteService
    {
        public IEnumerable<Quote> GetQuotes()
        {
            yield return new Quote("William Goldman", "Life isn't fair, it's just fairer than death, that's all.");
            yield return new Quote("Vizzini", "You fell victim to one of the classic blunders – The most famous of which is ‘never get involved in a land war in Asia'.");
            yield return new Quote("Westley", "We are men of action. Lies do not become us.");
            yield return new Quote("Vizzini", "Of all the necks on this boat your highness, the one you should be worrying about is your own.");
            yield return new Quote("Buttercup", "Poor and perfect… with eyes like the sea after a storm.");
            yield return new Quote("Inigo Montoya", "I just work for Vizzini to pay the bills. There's not a lot of money in revenge.");
            yield return new Quote("Prince Humperdink", "I've got my country's 500th anniversary to plan, my wedding to arrange, my wife to murder and Guilder to frame for it; I'm swamped.");
            yield return new Quote("Fezzik", "I just want you to feel you're doing well. I hate for people to die embarrassed.");
            yield return new Quote("Vizzini", "Inconceivable!");
            yield return new Quote("Westley", "You mean, you put down your rock and I put down my sword, and we try kill each other like civilized people?");
            yield return new Quote("Westley", "A few more steps and we'll be safe in the Fire Swamp!");
            yield return new Quote("Dread Pirate Roberts", "Good night, Westley. Good work. Sleep well. I'll most likely kill you in the morning.");
            yield return new Quote("Westley", "Learn to live with disappointment.");
            yield return new Quote("Fezzik", "Careful. People in masks cannot be trusted.");
        }
    }

    public class Quote
    {
        public Quote(string author, string text)
        {
            Author = author;
            Text = text;
        }

        public string Author { get; }

        public string Text { get; }
    }
}