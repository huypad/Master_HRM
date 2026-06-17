/* eslint-disable */
"use strict";

// Sử dụng biến toàn cục từ window thay vì import
var KTUtil = window.KTUtil;

var KTLayoutFooter = function() {
    // Private properties
    var _element;

    // Private functions
    var _getHeight = function() {
        var height = 0;

        if (_element) {
            height = KTUtil.actualHeight(_element);
        }

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
window.KTLayoutFooter = KTLayoutFooter;

// Loại bỏ export default để tránh xung đột
// export default KTLayoutFooter; // Đã comment hoặc xóa