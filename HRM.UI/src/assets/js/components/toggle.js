/* eslint-disable */
"use strict";

(function (window) {
    if (window.KTToggle) return;

    var KTUtil = window.KTUtil;

    function KTToggle(toggleElement, targetElement, options) {
        var the = this;
        var init = false;

        var element = toggleElement;
        var target = targetElement;

        if (!element) return;

        var defaultOptions = {
            targetToggleMode: 'class'
        };

        var Plugin = {
            construct: function (options) {
                if (KTUtil.data(element).has('toggle')) {
                    the = KTUtil.data(element).get('toggle');
                } else {
                    Plugin.init(options);
                    Plugin.build();
                    KTUtil.data(element).set('toggle', the);
                }
                return the;
            },
            init: function (options) {
                the.element = element;
                the.events = [];
                the.options = KTUtil.deepExtend({}, defaultOptions, options);
                the.target = target;
                the.targetState = the.options.targetState;
                the.toggleState = the.options.toggleState;

                if (the.options.targetToggleMode === 'class') {
                    the.state = KTUtil.hasClasses(the.target, the.targetState) ? 'on' : 'off';
                } else {
                    the.state = KTUtil.hasAttr(the.target, 'data-' + the.targetState) ? KTUtil.attr(the.target, 'data-' + the.targetState) : 'off';
                }
            },
            build: function () {
                KTUtil.addEvent(element, 'mouseup', Plugin.toggle);
            },
            toggle: function (e) {
                Plugin.eventTrigger('beforeToggle');
                if (the.state === 'off') {
                    Plugin.toggleOn();
                } else {
                    Plugin.toggleOff();
                }
                Plugin.eventTrigger('afterToggle');
                if (e) e.preventDefault();
                return the;
            },
            toggleOn: function () {
                Plugin.eventTrigger('beforeOn');
                if (the.options.targetToggleMode === 'class') {
                    KTUtil.addClass(the.target, the.targetState);
                } else {
                    KTUtil.attr(the.target, 'data-' + the.targetState, 'on');
                }
                if (the.toggleState) {
                    KTUtil.addClass(element, the.toggleState);
                }
                the.state = 'on';
                Plugin.eventTrigger('afterOn');
                Plugin.eventTrigger('toggle');
                return the;
            },
            toggleOff: function () {
                Plugin.eventTrigger('beforeOff');
                if (the.options.targetToggleMode === 'class') {
                    KTUtil.removeClass(the.target, the.targetState);
                } else {
                    KTUtil.removeAttr(the.target, 'data-' + the.targetState);
                }
                if (the.toggleState) {
                    KTUtil.removeClass(element, the.toggleState);
                }
                the.state = 'off';
                Plugin.eventTrigger('afterOff');
                Plugin.eventTrigger('toggle');
                return the;
            },
            eventTrigger: function (name) {
                for (var i = 0; i < the.events.length; i++) {
                    var event = the.events[i];
                    if (event.name === name) {
                        if (event.one === true && !event.fired) {
                            the.events[i].fired = true;
                            return event.handler.call(this, the);
                        } else if (!event.one) {
                            return event.handler.call(this, the);
                        }
                    }
                }
            },
            addEvent: function (name, handler, one) {
                the.events.push({
                    name: name,
                    handler: handler,
                    one: one,
                    fired: false
                });
                return the;
            }
        };

        the.setDefaults = function (options) {
            defaultOptions = options;
        };
        the.getState = function () {
            return the.state;
        };
        the.toggle = function () {
            return Plugin.toggle();
        };
        the.toggleOn = function () {
            return Plugin.toggleOn();
        };
        the.toggleOff = function () {
            return Plugin.toggleOff();
        };
        the.on = function (name, handler) {
            return Plugin.addEvent(name, handler);
        };
        the.one = function (name, handler) {
            return Plugin.addEvent(name, handler, true);
        };

        Plugin.construct.apply(the, [options]);
        return the;
    }

    window.KTToggle = KTToggle;
})(window);