
class SearchBarViewHelper {

    private textInput: HTMLInputElement;
    private searchButton: HTMLButtonElement;

    constructor() {
        this.textInput = document.getElementById("searchInput") as HTMLInputElement;
        this.searchButton = document.getElementById("searchButton") as HTMLButtonElement;
    }

    public run() {
        this.setupControls();
    }


    private setupControls() {

        var self = this;
        this.searchButton.onclick = function (ev: Event) {
            self.beginNewSearch();
            return false;
        }

        this.textInput.onkeydown = function (ev: KeyboardEvent) {
            if (ev.keyCode == 13) {
                self.beginNewSearch();
                return false;
            }
        }


    }

    private beginNewSearch() {
        var value = this.textInput.value.replace("\n", "");
        this.requestSearchAfterKeywords(value, "name");
    }

    private requestSearchAfterKeywords(keywords: string, where: string) {
        var url = window.location.origin + "/Explore/SearchPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=1";

        window.location.href = url;
    }
}

window.addEventListener("load", function () {
    var helper = new SearchBarViewHelper();
    helper.run();
    console.log('Search bar view loaded');
});

