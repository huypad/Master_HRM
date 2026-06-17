/* eslint-disable */
"use strict";

// Không dùng import nữa, lấy KTUtil từ window
var KTUtil = window.KTUtil;
// Nếu có dùng KTScrolltop là dạng global thì cũng lấy từ window, còn nếu vẫn dùng import thì sửa lại phần đó tương tự
var KTScrolltop = window.KTScrolltop;

var KTLayoutScrolltop = function() {
    // Private properties
    var _element;
    var _object;

    // Private functions
    var _init = function() {
        _object = new KTScrolltop(_element, {
            offset: 300,
            speed: 600,
        });
    }

    // Public methods
    return {
        init: function(id) {
            _element = KTUtil.getById(id);

            if (!_element) {
                return;
            }

            // Initialize
            _init();
        },

        getElement: function() {
            return _element;
        }
    };
}();

// Gán vào window để dùng toàn cục ở bất cứ đâu
window.KTLayoutScrolltop = KTLayoutScrolltop;

// Loại bỏ export default để tránh lỗi build
// export default KTLayoutScrolltop; // Không cần nữa!
