﻿;
(function (window, angular, undefined) {
    'use strict';

    function Service($http, $q, scConfig, uri) {

        function patchPromise(url, data, success, error) {

            var defer = $q.defer();

            success = success || 'success';
            error = error || 'error';

            $http({
                    url: url,
                    data: data,
                    method: 'PATCH'
                })
                .success(function (response) {
                    defer.resolve(success + ':' + response);
                })
                .error(function (response, status, headers, config) {
                    if (status === '304') {
                        defer.resolve(success + ':' + response);
                    } else {
                        defer.reject(error + ':' + response);
                    }
                });

            return defer.promise;
        }

        function getData() {
            var url = uri.join(scConfig.service_control_url, 'endpoints');
            return $http.get(url).then(function (response) {
                return {
                    data: response.data
                };
            });
        }

        var service = {
            getData: getData,
            update: function (id, newState, success, error) {
                var url = uri.join(scConfig.service_control_url, 'endpoints', id);
                return patchPromise(url, { "monitor_heartbeat": newState }, success, error);
            }
        };

        return service;

    }

    Service.$inject = ['$http', '$q', 'scConfig', 'uri'];

    angular.module('configuration')
        .factory('configurationService', Service);


}(window, window.angular));