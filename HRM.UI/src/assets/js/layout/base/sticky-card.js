/* eslint-disable */
"use strict";

// Thay import bằng window
var KTLayoutHeader = window.KTLayoutHeader;
var KTCard = window.KTCard;
var KTUtil = window.KTUtil;
var KTLayoutSubheader = window.KTLayoutSubheader;

// Component Definition
var KTLayoutStickyCard = function() {
    // Private properties
	var _element;
    var _object;

	// Private functions
	var _init = function() {
		var offset = 300;

		if (typeof KTLayoutHeader !== 'undefined') {
			offset = KTLayoutHeader.getHeight();
		}

        _object = new KTCard(_element, {
			sticky: {
				offset: offset,
				zIndex: 90,
				position: {
					top: function() {
						var pos = 0;
                        var body = KTUtil.getBody();

						if (KTUtil.isBreakpointUp('lg')) {
							if (typeof KTLayoutHeader !== 'undefined' && KTLayoutHeader.isFixed()) {
								pos = pos + KTLayoutHeader.getHeight();
							}

							if (typeof KTLayoutSubheader !== 'undefined' && KTLayoutSubheader.isFixed()) {
								pos = pos + KTLayoutSubheader.getHeight();
							}
						} else {
							if (typeof KTLayoutHeader !== 'undefined' && KTLayoutHeader.isFixedForMobile()) {
								pos = pos + KTLayoutHeader.getHeightForMobile();
							}
						}

						pos = pos - 1; // remove header border width

						return pos;
					},
					left: function(card) {
						return KTUtil.offset(_element).left;
					},
					right: function(card) {
						var body = KTUtil.getBody();

						var cardWidth = parseInt(KTUtil.css(_element, 'width'));
						var bodyWidth = parseInt(KTUtil.css(body, 'width'));
						var cardOffsetLeft = KTUtil.offset(_element).left;

						return bodyWidth - cardWidth - cardOffsetLeft;
					}
				}
			}
		});

		_object.initSticky();

		KTUtil.addResizeHandler(function() {
			_object.updateSticky();
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

		update: function() {
			if (_object) {
				_object.updateSticky();
			}
		}
	};
}();

// Gán vào window để sử dụng như biến toàn cục
window.KTLayoutStickyCard = KTLayoutStickyCard;

// Loại bỏ phần webpack và export không cần thiết
// if (typeof module !== 'undefined') {
//  module.exports = KTLayoutStickyCard;
// }
// export default KTLayoutStickyCard;