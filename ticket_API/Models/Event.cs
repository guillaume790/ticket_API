namespace ticket_API.Models
{
    public class Event
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public string Location { get; set; } = string.Empty;

        // Liste des tickets associés à l'événement
        public List<Ticket> Tickets { get; set; } = new();
    }
}
