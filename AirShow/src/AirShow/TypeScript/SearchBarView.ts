
class SearchBarViewHelper {

    private textInput: HTMLInputElement;
    private checkboxName: HTMLInputElement;
    private checkboxDescription: HTMLInputElement;
    private checkboxTags: HTMLInputElement;

    private searchButton: HTMLButtonElement;

    private anchorMyPresentations: HTMLAnchorElement;
    private anchorPublicPresentations: HTMLAnchorElement;

    constructor() {
        this.textInput = document.getElementById("searchInput") as HTMLInputElement;
        this.checkboxDescription = document.getElementById("checkboxDescription") as HTMLInputElement;
        this.checkboxName = document.getElementById("checkboxName") as HTMLInputElement;
        this.checkboxTags = document.getElementById("checkboxTags") as HTMLInputElement;
        this.anchorMyPresentations = document.getElementById("anchorMyPresentations") as HTMLAnchorElement;
        this.anchorPublicPresentations = document.getElementById("anchorPublicPresentations") as HTMLAnchorElement;

    }

    public run() {
        this.setupControls();
    }


    private setupControls() {

        var self = this;
        this.anchorMyPresentations.onclick = function (ev: Event) {
            self.beginNewSearch(true);
            return false;
        }

        this.anchorPublicPresentations.onclick = function (ev: Event) {
            self.beginNewSearch(false);
            return false;
        }

        this.textInput.onkeydown = function (ev: KeyboardEvent) {
            if (ev.keyCode == 13) {
                self.beginNewSearch(true);
                return false;
            }
        }


    }

    private beginNewSearch(personal: boolean) {
        var value = this.textInput.value.replace("\n", "");
        if (value.length == 0) {
            alert("Please insert at least one keyword for your search. Thank you");
            return;
        }
        if (personal) {
            this.requestSearchInMyPresentations(value, this.buildWhereString());
        } else {
            this.requestSearchInPublicPresentations(value, this.buildWhereString());
        }

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

    private requestSearchInMyPresentations(keywords: string, where: string) {
        var url = window.location.origin + "/Explore/SearchPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=5";

        window.location.href = url;
    }

    private requestSearchInPublicPresentations(keywords: string, where: string) {
        var url = window.location.origin + "/Explore/SearchPublicPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=5";

        window.location.href = url;
    }
}

window.addEventListener("load", function () {
    var helper = new SearchBarViewHelper();
    helper.run();
    console.log('Search bar view loaded');
});

