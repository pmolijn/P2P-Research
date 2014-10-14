FILTERDIR = './Filter/'
DLGSDIR = './Dialogues/'
AGGREGATESDIR = './Aggregate/'
TRAININGDIR = './training/'
PCAPFILES = 'PcapInputFiles.txt'
FILTEROPTIONS = 'FilterOptions.txt'
FLOWOUTFILE = FILTERDIR + 'FLOWDATA'
FLOWGAP = 1 * 60 * 60
THREADLIMIT = 10
UDP_HEADERLENGTH = 8

#utility functions
import os
def getCSVFiles(dirname):
	csvfiles = []
	for eachfile in os.listdir(dirname):
		if eachfile.endswith('.csv'):
			csvfiles.append(dirname + eachfile)	
	return csvfiles