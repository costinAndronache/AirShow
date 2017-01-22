var SearchBarViewHelper = (function () {
    function SearchBarViewHelper() {
        this.textInput = document.getElementById("searchInput");
        this.checkboxDescription = document.getElementById("checkboxDescription");
        this.checkboxName = document.getElementById("checkboxName");
        this.checkboxTags = document.getElementById("checkboxTags");
        this.anchorMyPresentations = document.getElementById("anchorMyPresentations");
        this.anchorPublicPresentations = document.getElementById("anchorPublicPresentations");
    }
    SearchBarViewHelper.prototype.run = function () {
        this.setupControls();
    };
    SearchBarViewHelper.prototype.setupControls = function () {
        var self = this;
        this.anchorMyPresentations.onclick = function (ev) {
            self.beginNewSearch(true);
            return false;
        };
        this.anchorPublicPresentations.onclick = function (ev) {
            self.beginNewSearch(false);
            return false;
        };
        this.textInput.onkeydown = function (ev) {
            if (ev.keyCode == 13) {
                self.beginNewSearch(true);
                return false;
            }
        };
    };
    SearchBarViewHelper.prototype.beginNewSearch = function (personal) {
        var value = this.textInput.value.replace("\n", "");
        if (value.length == 0) {
            alert("Please insert at least one keyword for your search. Thank you");
            return;
        }
        if (personal) {
            this.requestSearchInMyPresentations(value, this.buildWhereString());
        }
        else {
            this.requestSearchInPublicPresentations(value, this.buildWhereString());
        }
    };
    SearchBarViewHelper.prototype.buildWhereString = function () {
        var whereString = "";
        if (this.checkboxName.checked) {
            whereString += "name";
        }
        if (this.checkboxDescription.checked) {
            if (whereString.length > 0) {
                whereString += ",";
            }
            whereString += "description";
        }
        if (this.checkboxTags.checked) {
            if (whereString.length > 0) {
                whereString += ",";
            }
            whereString += "tags";
        }
        if (whereString.length == 0) {
            whereString = "name";
        }
        return whereString;
    };
    SearchBarViewHelper.prototype.requestSearchInMyPresentations = function (keywords, where) {
        var url = window.location.origin + "/Explore/SearchPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=5";
        window.location.href = url;
    };
    SearchBarViewHelper.prototype.requestSearchInPublicPresentations = function (keywords, where) {
        var url = window.location.origin + "/Explore/SearchPublicPresentations?keywords=" + encodeURI(keywords) +
            "&where" + where + "&page=1&itemsPerPage=5";
        window.location.href = url;
    };
    return SearchBarViewHelper;
}());
window.addEventListener("load", function () {
    var helper = new SearchBarViewHelper();
    helper.run();
    console.log('Search bar view loaded');
});
//# sourceMappingURL=SearchBarView.js.map