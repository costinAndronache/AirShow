
class SearchBarViewHelper {

    private textInput: HTMLInputElement;
    private checkboxName: HTMLInputElement;
    private checkboxDescription: HTMLInputElement;
    private checkboxTags: HTMLInputElement;

    private searchButton: HTMLButtonElement;
   

    constructor() {
        this.textInput = document.getElementById("searchInput") as HTMLInputElement;
        this.searchButton = document.getElementById("searchButton") as HTMLButtonElement;
        this.checkboxDescription = document.getElementById("checkboxDescription") as HTMLInputElement;
        this.checkboxName = document.getElementById("checkboxName") as HTMLInputElement;
        this.checkboxTags = document.getElementById("checkboxTags") as HTMLInputElement;
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
        this.requestSearchAfterKeywords(value, this.buildWhereString());
    }

    private buildWhereString(): string {
        var whereString = "";

        if (this.checkboxName.checked) {
            whereString += "name";
        }

        if (this.checkboxDescription.checked) {
            if (whereString.length > 0) { whereString += ","; }
            whereString += "description";
        }

        if (this.checkboxTags.checked) {
            if (whereString.length > 0) { whereString += "," }
            whereString += "tags";
        }

        if (whereString.length == 0) { whereString = "name"; }
        return whereString;
    }

    private requestSearchAfterKeywords(keywords: string, where: string) {
        var url = window.location.origin + "/Explore/SearchPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=1";

        alert(url);
        window.location.href = url;
    }
}

window.addEventListener("load", function () {
    var helper = new SearchBarViewHelper();
    helper.run();
    console.log('Search bar view loaded');
});

