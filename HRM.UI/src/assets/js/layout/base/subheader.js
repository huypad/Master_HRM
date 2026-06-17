/* eslint-disable */
"use strict";

// Sử dụng biến toàn cục từ window thay vì import
var KTUtil = window.KTUtil;

var KTLayoutSubheader = function() {
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

            if (!_element) {
                return;
            }
        },

        isFixed: function() {
            return KTUtil.hasClass(KTUtil.getBody(), 'subheader-fixed');
        },

        getElement: function() {
            return _element;
        },

        getHeight: function() {
            return _getHeight();
        }
    };
}();

// Gán vào window để sử dụng như biến toàn cục
window.KTLayoutSubheader = KTLayoutSubheader;

// Loại bỏ export default để tránh xung đột
// export default KTLayoutSubheader; // Đã comment hoặc xóa