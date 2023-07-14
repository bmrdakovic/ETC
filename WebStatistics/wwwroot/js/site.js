// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var app = angular.module('StatisticsAppController', ['ui.bootstrap']);
app.run(function () { });

app.controller('StatisticsAppController', ['$rootScope', '$scope', '$http', '$timeout', ($rootScope, $scope, $http, $timeout) => {
    $scope.refresh = () =>
        $http.get('api/load')
            .then(
                (data, status) => $scope.statistics = data,
                (data, status) => $scope.statistics = undefined);
    
}]);