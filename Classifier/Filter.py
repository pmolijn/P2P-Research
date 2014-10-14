## usage: python Filter.py

#import global constants
from GLOBALS import *
from FilterPacketsHelper import *
import multiprocessing as MP
import subprocess

global sem
sem = MP.Semaphore(THREADLIMIT)

#execute a shell command as a child process
def executeCommand(command,outfilename):
	global sem
	sem.acquire()

	subprocess.call(command, shell = True)
	
	infile = open(outfilename, 'r')
	data = [eachline.strip() for eachline in infile]
	infile.close()
	
	data = preprocess(data)
	
	outfile = open(outfilename,'w')
	for eachcomponent in data:
		outfile.write(eachcomponent)
	outfile.close()
	
	print 'done processing : ' + outfilename
	sem.release()

#obtain input parameters and pcapfilenames
inputfiles = getPCapFileNames()
tsharkOptions = getTsharkOptions()

if __name__ == '__main__':
	MP.freeze_support()

	#create a semaphore so as not to exceed threadlimit
	#global sem
	#sem = MP.Semaphore(THREADLIMIT)

	#get tshark commands to be executed
	for filename in inputfiles:
		print filename
		(command,outfilename) = 	contructTsharkCommand(filename,tsharkOptions)
		task = MP.Process(target = executeCommand, args = (command, outfilename,))
		task.start()