using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using BotTemplate.Managers;
using BotTemplate.Objects.Sql;

namespace BotTemplate.Modules {
	public class MiscModule : BaseCommandModule {
		[Command("newnote")]
		public async Task NewNoteAsync(CommandContext ctx, string name, [RemainingText] string value) {
			if (await NoteManager.GetAsync(name) != null) /* already exists */
				throw new UserException("you already have a note");

			var note = new Note() {
				Name = name,
				Author = ctx.User.Id,
				Value = value
			};
			await NoteManager.SyncAsync(note);
			await ctx.RespondAsync($"Done. view with `noteview {name}`");
		}

		[Command("noteview")]
		public async Task NoteViewAsync(CommandContext ctx, string name) {
			var note = await NoteManager.GetAsync(name);
			if (note == null)
				throw new UserException($"No such note: {name}");

			await ctx.RespondAsync($"\"{note.Value}\" by <@{note.Author}>");
		}
	}
}
