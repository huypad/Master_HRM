/* eslint-disable */
"use strict";

(function(window) {
    // Nếu đã tồn tại thì không ghi đè
    if (window.KTOffcanvas) return;

    var KTUtil = window.KTUtil;

    function KTOffcanvas(elementId, options) {
        var the = this;
        var init = false;

        var element = KTUtil.getById(elementId);
        var body = KTUtil.getBody();

        if (!element) return;

        var defaultOptions = {
            attrCustom: ''
        };

        var Plugin = {
            construct: function(options) {
                if (KTUtil.data(element).has('offcanvas')) {
                    the = KTUtil.data(element).get('offcanvas');
                } else {
                    Plugin.init(options);
                    Plugin.build();
                    KTUtil.data(element).set('offcanvas', the);
                }
                return the;
            },
            init: function(options) {
                the.events = [];
                the.options = KTUtil.deepExtend({}, defaultOptions, options);
                the.classBase = the.options.baseClass;
                the.attrCustom = the.options.attrCustom;
                the.classShown = the.classBase + '-on';
                the.classOverlay = the.classBase + '-overlay';
                the.target;
                the.state = KTUtil.hasClass(element, the.classShown) ? 'shown' : 'hidden';
            },
            build: function() {
                if (the.options.toggleBy) {
                    if (typeof the.options.toggleBy === 'string') {
                        KTUtil.addEvent(KTUtil.getById(the.options.toggleBy), 'click', function(e) {
                            e.preventDefault();
                            the.target = this;
                            Plugin.toggle();
                        });
                    } else if (the.options.toggleBy && the.options.toggleBy[0]) {
                        if (the.options.toggleBy[0].target) {
                            for (var i in the.options.toggleBy) {
                                KTUtil.addEvent(KTUtil.getById(the.options.toggleBy[i].target), 'click', function(e) {
                                    e.preventDefault();
                                    the.target = this;
                                    Plugin.toggle();
                                });
                            }
                        } else {
                            for (var i in the.options.toggleBy) {
                                KTUtil.addEvent(KTUtil.getById(the.options.toggleBy[i]), 'click', function(e) {
                                    e.preventDefault();
                                    the.target = this;
                                    Plugin.toggle();
                                });
                            }
                        }
                    } else if (the.options.toggleBy && the.options.toggleBy.target) {
                        KTUtil.addEvent(KTUtil.getById(the.options.toggleBy.target), 'click', function(e) {
                            e.preventDefault();
                            the.target = this;
                            Plugin.toggle();
                        });
                    }
                }

                var closeBy = KTUtil.getById(the.options.closeBy);
                if (closeBy) {
                    KTUtil.addEvent(closeBy, 'click', function(e) {
                        e.preventDefault();
                        the.target = this;
                        Plugin.hide();
                    });
                }
            },
            isShown: function() {
                return the.state == 'shown';
            },
            toggle: function() {
                Plugin.eventTrigger('toggle');
                if (the.state == 'shown') {
                    Plugin.hide();
                } else {
                    Plugin.show();
                }
            },
            show: function() {
                if (the.state == 'shown') return;
                Plugin.eventTrigger('beforeShow');
                Plugin.toggleClass('show');
                KTUtil.attr(body, 'data-offcanvas-' + the.classBase, 'on');
                KTUtil.addClass(element, the.classShown);
                if (the.attrCustom.length > 0) {
                    KTUtil.attr(body, 'data-offcanvas-' + the.classCustom, 'on');
                }
                the.state = 'shown';
                if (the.options.overlay) {
                    the.overlay = KTUtil.insertAfter(document.createElement('DIV'), element);
                    KTUtil.addClass(the.overlay, the.classOverlay);
                    KTUtil.addEvent(the.overlay, 'click', function(e) {
                        e.preventDefault();
                        Plugin.hide(the.target);
                    });
                }
                Plugin.eventTrigger('afterShow');
            },
            hide: function() {
                if (the.state == 'hidden') return;
                Plugin.eventTrigger('beforeHide');
                Plugin.toggleClass('hide');
                KTUtil.removeAttr(body, 'data-offcanvas-' + the.classBase);
                KTUtil.removeClass(element, the.classShown);
                if (the.attrCustom.length > 0) {
                    KTUtil.removeAttr(body, 'data-offcanvas-' + the.attrCustom);
                }
                the.state = 'hidden';
                if (the.options.overlay && the.overlay) {
                    KTUtil.remove(the.overlay);
                }
                Plugin.eventTrigger('afterHide');
            },
            toggleClass: function(mode) {
                var id = KTUtil.attr(the.target, 'id');
                var toggleBy;
                if (the.options.toggleBy && the.options.toggleBy[0] && the.options.toggleBy[0].target) {
                    for (var i in the.options.toggleBy) {
                        if (the.options.toggleBy[i].target === id) {
                            toggleBy = the.options.toggleBy[i];
                        }
                    }
                } else if (the.options.toggleBy && the.options.toggleBy.target) {
                    toggleBy = the.options.toggleBy;
                }
                if (toggleBy) {
                    var el = KTUtil.getById(toggleBy.target);
                    if (mode === 'show') KTUtil.addClass(el, toggleBy.state);
                    if (mode === 'hide') KTUtil.removeClass(el, toggleBy.state);
                }
            },
            eventTrigger: function(name, args) {
                for (var i = 0; i < the.events.length; i++) {
                    var event = the.events[i];
                    if (event.name == name) {
                        if (event.one === true) {
                            if (!event.fired) {
                                the.events[i].fired = true;
                                return event.handler.call(this, the, args);
                            }
                        } else {
                            return event.handler.call(this, the, args);
                        }
                    }
                }
            },
            addEvent: function(name, handler, one) {
                the.events.push({
                    name: name,
                    handler: handler,
                    one: one,
                    fired: false
                });
            }
        };

        the.setDefaults = function(options) {
            defaultOptions = options;
        };
        the.isShown = function() {
            return Plugin.isShown();
        };
        the.hide = function() {
            return Plugin.hide();
        };
        the.show = function() {
            return Plugin.show();
        };
        the.on = function(name, handler) {
            return Plugin.addEvent(name, handler);
        };
        the.one = function(name, handler) {
            return Plugin.addEvent(name, handler, true);
        };

        Plugin.construct.apply(the, [options]);
        init = true;
        return the;
    }

    // Gán vào window
    window.KTOffcanvas = KTOffcanvas;
})(window);
