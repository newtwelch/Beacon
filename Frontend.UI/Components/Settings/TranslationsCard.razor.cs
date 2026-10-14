using Backend.Core.Services;

namespace Frontend.UI.Components.Settings
{
    public partial class TranslationsCard
    {

        private List<Backend.Core.Models.Bible.Bible> InstalledTranslations = new();

        private string Downloading = "";
        private int DownloadProgress;

        private readonly List<AvailableTranslation> AvailableTranslations = new()
    {
        new()
        {
            Name = "King James Version",
            Abbreviation = "kjv"
        },

        new()
        {
            Name = "Tagalog",
            Abbreviation = "tagalog"
        },

    };

        protected override async Task OnInitializedAsync()
        {
            await RefreshTranslations();
        }

        private async Task RefreshTranslations()
        {
            InstalledTranslations =
                await BibleService.GetBiblesAsync();
        }

        private async Task DownloadTranslation(
            AvailableTranslation translation)
        {
            if (!string.IsNullOrEmpty(Downloading))
                return;

            Downloading = translation.Abbreviation;
            DownloadProgress = 0;

            await InvokeAsync(StateHasChanged);

            try
            {
                DownloadProgress = 25;
                await InvokeAsync(StateHasChanged);

                var success =
                    await BibleService.DownloadBible(
                        translation.Abbreviation);

                await Task.Delay(200);
                DownloadProgress = 40;
                await Task.Delay(200);
                DownloadProgress = 80;
                await Task.Delay(200);
                DownloadProgress = 100;

                if (success)
                    await RefreshTranslations();

            }
            finally
            {
                Downloading = "";

                await InvokeAsync(StateHasChanged);

            }
        }

        private async Task DeleteTranslation(
            AvailableTranslation translation)
        {
            await BibleService.DeleteBibleAsync(
                translation.Abbreviation);

            await RefreshTranslations();

            await InvokeAsync(StateHasChanged);

        }

        private class AvailableTranslation
        {
            public string Name { get; set; } = "";
            public string Abbreviation { get; set; } = "";
        }
    }
}