using Beacon.Model.Enum;
using Beacon.Model.Songs;
using Microsoft.JSInterop;

namespace Beacon.Blazor.Pages
{
	public partial class SongLibrary
	{
		private List<Song> songs = new List<Song>();
		private List<Song> languagesOfSelectedSong = new List<Song>();
		private Song selectedSong = new Song();
		private Song selectedSongClone = new Song();
		private Song songToDelete = new Song();
		private Lyric selectedLyric = new Lyric();
		private List<Lyric> lyrics = new List<Lyric>();

		private SearchMode currentSearchMode;
		private bool isQueueMode = false;
		private bool editMode = false;
		private bool showDeleteModal = false;

		private string searchText = "";

		protected override async Task OnInitializedAsync()
		{
			songs = await SongService.GetAllAsync();
		}

		private async Task switchSearchMode(SearchMode searchMode)
		{
			currentSearchMode = searchMode;
			await OnSearchChanged(searchText);
		}

		private async Task SelectedSongChanged(Song song)
		{
			selectedSong = await SongService.GetAsync(song.Id);
			languagesOfSelectedSong = await SongService.GetLanguagesAsync(selectedSong.Number);
			UpdateLyrics();
		}

		private void UpdateLyrics() => lyrics = selectedSong.Lyrics();

		private async Task OnSearchChanged(string value)
		{
			if (String.IsNullOrWhiteSpace(searchText))
			{
				songs = await SongService.GetAllAsync();
				return;
			}

			songs = currentSearchMode switch
			{
				SearchMode.Title => await SongService.QueryTitleAsync(searchText),
				SearchMode.Lyric => await SongService.QueryLyricAsync(searchText),
				_ => await SongService.GetAllAsync()
			};
		}

		private void EditButtonClick()
		{
			editMode = true;
			selectedSongClone = new Song(selectedSong);
		}

		private async Task SaveEditClick()
		{
			editMode = false;

			// Update the local list so we don't have to fetch from database
			var index = songs.FindIndex(s => selectedSong.Id == s.Id);
			songs[index] = new Song(selectedSong);
			songs[index].LyricText = "";

			await SongService.UpdateAsync(selectedSong);
		}

		private void DiscardEditClick()
		{
			editMode = false;
			selectedSong = new Song(selectedSongClone);
			UpdateLyrics();
		}

		private async Task AddSongClick()
		{
			await AddSong(new Song("Song Title", "Song Author"));
		}

		private async Task AddLanguageClick()
		{
			await AddSong(new Song(selectedSong.Number, selectedSong.Title, selectedSong.Author));
		}

		private async Task SongDeleteClick(Song song)
		{
			songToDelete = song;
			
			if (true) // add a bool in setting for "Always verify song deletion"
			{
				showDeleteModal = true;
			}
			else
			{
				await DeleteSong();
			}
		}

		private async Task DeleteSong()
		{
			songs.Remove(songToDelete);
			await SongService.DeleteAsync(songToDelete);
			songToDelete = new Song();
			showDeleteModal = false;
		}

		private async Task AddSong(Song song)
		{
			var newSong = await SongService.AddAsync(song);
			songs.Add(newSong);
			await SelectedSongChanged(newSong);

			await InvokeAsync(StateHasChanged);
			await jSRuntime.InvokeVoidAsync("ScrollWithHeight", "SongList", songs.Count());
		}

		private void SelectedLyricChanged(Lyric lyric)
		{
			selectedLyric = lyric;
		}
	}
}