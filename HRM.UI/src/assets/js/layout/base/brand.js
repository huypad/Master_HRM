/* eslint-disable */
"use strict";

// KHÔNG import nữa, dùng global từ window
var KTUtil = window.KTUtil;

var KTLayoutBrand = function() {
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

        getElement: function() {
            return _element;
        },

        getHeight: function() {
            return _getHeight();
        }
    };
}();

// Gán lên window để có thể truy cập global ở mọi file script khác
window.KTLayoutBrand = KTLayoutBrand;

// Không export default nữa để không bị lỗi khi build Angular
// export default KTLayoutBrand;
