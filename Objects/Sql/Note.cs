namespace BotTemplate.Objects.Sql {
	/* A note object, it's simple and has no
	 * logic; it only stores data. */
	public class Note {
		public string Name { get; set; }
		public ulong Author { get; set; }
		public string Value { get; set; }
	}
}
