using SmartX.Api.Data;

namespace SmartX.Api.Models
{
	/*interface - specifies what a class or struct must do. It's a blueprint for behaviour.
	Allows different data types to be treated identically, if they are processed through 
	this task function (Polymorphism).
	
	By naming convention, interface names always start with an "I". */
    public interface IStructProcessor<T> where T : struct
    {
        Task ProcessAndSaveAsync(T data, int sensorId, string unit);
    } 
	
	/*: - inheritance/implementation of the IStructProcessor interface
	
	internal - only accessible within this class. The user doesn't need to
	know how the readings are processed, only that they're processing.
	
	Prevents the overriding of anything inside it by the rest of the project.
	*/

    internal class TelemetryPacketProcessor<T> : IStructProcessor<T> where T : struct
    {
        private readonly SmartXDbContext _context;
		
		/* Instead of creating a new database connection manually, a constructor 
		injects an existing database connection. It allows you to use _context
		whenever you want to query, update, or delete data from your database. 
		
		
		Constructor - method that runs automatically whenever a new instance of this 
		class is created.
		
		Parameter (SmartXDbContext context) - An instance of SmartXDbContext must be 
		provided in order to use this class.
		
		_context = context - takes the database instance that was passed in (context) and 
		assigns it to a private, class-level variable (SmartXDbContext _context). 
		*/

        public TelemetryPacketProcessor(SmartXDbContext context)
        {
            _context = context;
        }

	/*Task - return type. Since it's async, it returns a Task (reps the ongoing background 
	work) instead of void. The "Async" in ProcessAndSaveAsync is C# naming convention 
	that communicates this method needs to be awaited.
	
	(T data, int sensorId, string unit) - values expected from the client that are to be persisted
	
	*/
        public async Task ProcessAndSaveAsync(T data, int sensorId, string unit)
        {
			/* var - implicit type declaration. 
			new TelemetryReading - constructor call creating a new instance of the class TelemetryReading
			Compiler infers the type based on the " new TelemetryReading" contstructor call
			
			
			*/
            var reading = new TelemetryReading
            {
                SensorId = sensorId,
                Timestamp = DateTime.UtcNow,
				/*?? - null-coalescing operator. It evaluates data.ToString() and returns it 
				if it's not null. If data.ToString() is null, it falls back and returns string.Empty()*/
                Value = data.ToString() ?? string.Empty,
				
				/*typeof(T) -returns an object that reps a generic type
				
				.Name - returns the name of the type as a string ("int", "bool" etc.) 
				*/
				
                ValueType = typeof(T).Name,
                Unit = unit
            };

            _context.Readings.Add(reading);
            await _context.SaveChangesAsync();
        }
    }
}
