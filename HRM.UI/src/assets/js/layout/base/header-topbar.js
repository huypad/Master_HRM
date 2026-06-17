/* eslint-disable */
"use strict";

// Sử dụng biến toàn cục từ window thay vì import
var KTToggle = window.KTToggle;
var KTUtil = window.KTUtil;

var KTLayoutHeaderTopbar = function() {
    // Private properties
    var _toggleElement;
    var _toggleObject;

    // Private functions
    var _init = function() {
        _toggleObject = new KTToggle(_toggleElement, KTUtil.getBody(), {
            targetState: 'topbar-mobile-on',
            toggleState: 'active',
        });
    }

    // Public methods
    return {
        init: function(id) {
            _toggleElement = KTUtil.getById(id);

            if (!_toggleElement) {
                return;
            }

            // Initialize
            _init();
        },

        getToggleElement: function() {
            return _toggleElement;
        }
    };
}();

// Gán vào window để sử dụng như biến toàn cục
window.KTLayoutHeaderTopbar = KTLayoutHeaderTopbar;

// Loại bỏ export default để tránh xung đột
// export default KTLayoutHeaderTopbar; // Đã comment hoặc xóa