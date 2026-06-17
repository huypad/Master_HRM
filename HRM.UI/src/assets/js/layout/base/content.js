/* eslint-disable */
"use strict";

// Thay import bằng sử dụng window
var KTUtil = window.KTUtil;
var KTLayoutHeader = window.KTLayoutHeader;
var KTLayoutSubheader = window.KTLayoutSubheader;
var KTLayoutFooter = window.KTLayoutFooter;

var KTLayoutContent = function() {
    // Private properties
    var _element;

    // Private functions
    var _getHeight = function() {
        var height;

        height = KTUtil.getViewPort().height;

        if (_element) {
            height = height - parseInt(KTUtil.css(_element, 'paddingTop')) - parseInt(KTUtil.css(_element, 'paddingBottom'));
        }

        height = height - KTLayoutHeader.getHeight();
        height = height - KTLayoutSubheader.getHeight();
        height = height - KTLayoutFooter.getHeight();

        return height;
    }

    // Public methods
    return {
        init: function(id) {
            _element = KTUtil.getById(id);
        },

        getHeight: function() {
            return _getHeight();
        },

        getElement: function() {
            return _element;
        }
    };
}();

// Gán vào window để sử dụng như biến toàn cục
window.KTLayoutContent = KTLayoutContent;

// Loại bỏ export default để tránh xung đột
// export default KTLayoutContent; // Comment hoặc xóa dòng này