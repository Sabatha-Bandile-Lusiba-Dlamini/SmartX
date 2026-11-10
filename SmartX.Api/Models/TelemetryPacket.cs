namespace SmartX.Api.Models

{
	
	/*
	"where T: struct" makes reference types fail at compile time.
	A reading can never be a reference type or null (have no value),
	and structs are by default non-nullable because they hold actual 
	data on the stack (telemetry readings), so there's no underlying 
	pointer to set to null. This is because, again, the data is on the 
	stack, not in the heap. You only point to data if it's stored in the 
	heap.
	
	Instead of null, the default state of a struct is its binary representation 
	of 0 (0 for ints, false for bools etc.). This is why initializing default 
	structs (receiving telemetry data) at runtime is fast. The reason it's fast 
	is because it takes a single CPU operation to clear a bunch of zeros.
	There's no dynamic memory allocation, garbage collection tracking, or 
	pointer tracking.
	
	Structs are designed for high performance and lightweight data structures.
	A struct takes up the exact amount of bytes required for its data, 
	nothing more. This is why any type that's dynamic is inherently a reference 
	type in C# (with regards to memory allocation). 
	
	Structs have great cache locality. This is the program's ability to 
	repeatedly access familiar/close memory locations in a short time frame.
	Basically, if a program accesses a specific memory address, that means 
	it can access nearby memory addresses quickly.
	
	
	*/
    public class TelemetryPacket<T> where T : struct
    {
        public int SensorId { get; set; }
        public T Value { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}