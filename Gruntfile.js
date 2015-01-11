var devserverConfig = require('./grunt/devserver.json');

module.exports = function(grunt){

    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),
        devserver: devserverConfig
    });

    grunt.loadNpmTasks('grunt-devserver');

}
