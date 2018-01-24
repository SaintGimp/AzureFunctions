var request = require('request');
var Particle = require('particle-api-js');
var particle = new Particle();

module.exports = function (context, myTimer) {
    context.log('Getting current weather...');
    
    var options = {
      url: 'http://api.wunderground.com/api/'+ process.env.WEATHERUNDERGROUND_API_KEY +'/forecast/q/'+ process.env.WEATHER_LOCATION +'.json',
      json: true
    };
    
    request(options, function (error, response, body) {
      if (!error && response.statusCode == 200) {
        context.log('The raw forecast is:');
        context.log(body.forecast.simpleforecast.forecastday[0]);
    
        var forecast = parseForecast(body);
        context.log('The forecast to display is ' + forecast);
    
        context.log('Sending to device...');
    
        particle.callFunction({
          deviceId: process.env.WEATHER_PARTICLE_DEVICE_ID,
          name: 'display', argument: forecast,
          auth: process.env.WEATHER_PARTICLE_ACCESS_KEY
        })
        .then(
          function(data) {
            context.log('Done.');
          }, function(err) {
            context.log('An error occurred:', err);
          })
        .then(function() {
            context.done();
        });
      }
      else {
        context.log('An error occurred:', error);
        context.done();
      }
    });
};

function parseForecast(body) {
  var forecast = body.forecast.simpleforecast.forecastday[0].icon;
  switch(forecast) {
    case 'clear':
    case 'sunny':
    case 'mostlysunny':
      return 'sunny';

    case 'partlycloudy':
    case 'hazy':
    case 'partlysunny' :
      return 'partlycloudy';

    case 'cloudy':
    case 'mostlycloudy':
    case 'fog':
      return 'cloudy';

    case 'chanceflurries':
    case 'chancerain':
    case 'chancesleet':
    case 'chancesnow':
    case 'chancetstorms':
    case 'flurries':
    case 'sleet':
    case 'rain':
    case 'snow':
    case 'tstorms':
    case 'unknown':
      return 'rain';
    
    default:
      return 'rain';
  }
}
