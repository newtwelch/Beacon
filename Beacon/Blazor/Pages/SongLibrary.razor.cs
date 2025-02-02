using Beacon.Model.Enums;
using Beacon.Model.Songs;
using Beacon.WPF;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Windows;

namespace Beacon.Blazor.Pages
{
    public partial class SongLibrary
	{
		[Parameter] public Func<bool> ShouldReRender { get; set; }

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

			if (!settingsService.settings.EnableSearchModeUI)
            {
				if (searchText.StartsWith("."))
					songs = await SongService.QueryLyricAsync(searchText.Substring(1));
				else if(searchText.StartsWith("*"))
                    songs = await SongService.QueryAuthorAsync(searchText.Substring(1));
				else if (searchText.StartsWith("#"))
					songs = await SongService.QueryTagAsync(searchText.Substring(1));
				else
					songs = await SongService.QueryTitleAsync(searchText);

                return;
            }

			songs = currentSearchMode switch
			{
				SearchMode.Title => await SongService.QueryTitleAsync(searchText),
				SearchMode.Lyric => await SongService.QueryLyricAsync(searchText),
				SearchMode.Author => await SongService.QueryAuthorAsync(searchText),
				SearchMode.Tag => await SongService.QueryTagAsync(searchText),
				_ => await SongService.GetAllAsync()
			};
		}

		private async Task QueueModeToggle()
		{
			isQueueMode = !isQueueMode;

			if (isQueueMode) 
			{
				songs = await SongService.GetQueueAsync();
			}
			else
			{
				songs = await SongService.GetAllAsync();
			}
		}
		private void EditButtonClick()
		{
			editMode = true;
			selectedSongClone = new Song(selectedSong);
		}
		private void DiscardEditClick()
		{
			editMode = false;
			selectedSong = new Song(selectedSongClone);
			UpdateLyrics();
		}
		private async Task SaveEditClick()
		{
			editMode = false;

			await SongService.UpdateAsync(selectedSong);

			// Update the local list so we don't have to fetch from database
			var index = songs.FindIndex(s => selectedSong.Id == s.Id);
			songs[index] = new Song(selectedSong);
			songs[index].LyricText = "";
			songs[index].InQueue = selectedSong.InQueue;

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
			
			if (settingsService.settings.AlwaysConfirmSongDeletion) // add a bool in setting for "Always verify song deletion"
			{
				showDeleteModal = true;
			}
			else
			{
				await DeleteSong();
			}
		}
		private async Task AddToQueueClick(Song song)
		{
			song.InQueue = true;

			var songToAdd = await SongService.GetAsync(song.Id);
			songToAdd.InQueue = true;
			songToAdd.QueueOrder = await SongService.GetHighestQueueOrderAsync() + 1;
			await SongService.UpdateAsync(songToAdd);
		}
		private async Task RemoveFromQueueClick(Song song)
		{
			song.InQueue = false;

			var songToRemove = await SongService.GetAsync(song.Id);
			songToRemove.InQueue = false;
			songToRemove.QueueOrder = 0;
			await SongService.UpdateAsync(songToRemove);

			if (isQueueMode)
			{
				songs.Remove(song);
			}
		}
		private async Task OnSearchKeyPress(KeyboardEventArgs e)
		{
			//if (e.Code == "Enter")
			//{
			//	await SelectedSongChanged(songs[0]);
   //             await jSRuntime.InvokeVoidAsync("SwitchFocusTo", "SongList");
   //         }
		}
		private async Task ClearQueue()
		{
			if (!isQueueMode) return;

			foreach(var song in songs)
			{
				var songToRemove = await SongService.GetAsync(song.Id);
				songToRemove.InQueue = false;
				songToRemove.QueueOrder = 0;
				await SongService.UpdateAsync(songToRemove);
			}

			songs.Clear();
		}

		private async Task DeleteSong()
		{
			songs.Remove(songToDelete);
			await SongService.DeleteAsync(songToDelete);
			songToDelete = new Song();
			showDeleteModal = false;
			lyrics.Clear();

			selectedSong = new Song();
		}
		private async Task AddSong(Song song)
		{
			var newSong = await SongService.AddAsync(song);
			songs.Add(newSong);
			await SelectedSongChanged(newSong);

			await InvokeAsync(StateHasChanged);
			await jSRuntime.InvokeVoidAsync("ScrollWithHeight", "SongList", songs.Count());
		}
		private async Task SelectedLyricChanged(Lyric lyric)
		{
			selectedLyric = lyric;

			await InvokeAsync(() =>
			{
				DisplayWindow.Instance.SetWindow(settingsService.settings.ProjectionMonitor);
				DisplayWindow.Instance.Show();
				DisplayWindow.Instance.Content.Text = lyric.Text.RemoveHighlight();
				DisplayWindow.Instance.Content.FontSize = 100;
				DisplayWindow.Instance.Content.TextWrapping = TextWrapping.NoWrap;
				DisplayWindow.Instance.Content.Width = Double.NaN;
				DisplayWindow.Instance.Content.HighlightCount = 0;
				DisplayWindow.Instance.Header1.Text = selectedSong.Title.ToUpper();
				DisplayWindow.Instance.Header2.Text = lyric.Id + " / " + selectedSong.Lyrics().Count();
			});
        }
	}
}