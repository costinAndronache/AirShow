var SearchBarViewHelper = (function () {
    function SearchBarViewHelper() {
        this.textInput = document.getElementById("searchInput");
        this.searchButton = document.getElementById("searchButton");
        this.checkboxDescription = document.getElementById("checkboxDescription");
        this.checkboxName = document.getElementById("checkboxName");
        this.checkboxTags = document.getElementById("checkboxTags");
    }
    SearchBarViewHelper.prototype.run = function () {
        this.setupControls();
    };
    SearchBarViewHelper.prototype.setupControls = function () {
        var self = this;
        this.searchButton.onclick = function (ev) {
            self.beginNewSearch();
            return false;
        };
        this.textInput.onkeydown = function (ev) {
            if (ev.keyCode == 13) {
                self.beginNewSearch();
                return false;
            }
        };
    };
    SearchBarViewHelper.prototype.beginNewSearch = function () {
        var value = this.textInput.value.replace("\n", "");
        this.requestSearchAfterKeywords(value, this.buildWhereString());
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
    SearchBarViewHelper.prototype.requestSearchAfterKeywords = function (keywords, where) {
        var url = window.location.origin + "/Explore/SearchPresentations?keywords=" + encodeURI(keywords) +
            "&where=" + where + "&page=1&itemsPerPage=1";
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